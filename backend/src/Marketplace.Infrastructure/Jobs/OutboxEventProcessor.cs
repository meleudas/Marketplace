using System.Text.Json;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;

namespace Marketplace.Infrastructure.Jobs;

public sealed class OutboxEventProcessor : IOutboxEventProcessor
{
    private readonly IInboxDeduplicator _inbox;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOrderCacheInvalidationService _cacheInvalidation;
    private readonly IOrderStatusHistoryWriter _historyWriter;

    public OutboxEventProcessor(
        IInboxDeduplicator inbox,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IOrderPaymentStateApplier paymentStateApplier,
        IOrderCacheInvalidationService cacheInvalidation,
        IOrderStatusHistoryWriter historyWriter)
    {
        _inbox = inbox;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _paymentStateApplier = paymentStateApplier;
        _cacheInvalidation = cacheInvalidation;
        _historyWriter = historyWriter;
    }

    public async Task ProcessAsync(OutboxMessage message, CancellationToken ct = default)
    {
        switch (message.EventType)
        {
            case "PaymentStatusChanged":
                await ProcessPaymentStatusChangedAsync(message, ct);
                break;
            case "OrderCreated":
            case "OrderCancelled":
            case "OrderStatusChanged":
                await ProcessOrderInvalidationAsync(message, ct);
                break;
            case "InventoryReserved":
            case "InventoryReleased":
            case "InventoryFailed":
                await MarkInventoryEventAsSeenAsync(message, ct);
                break;
        }
    }

    private async Task ProcessPaymentStatusChangedAsync(OutboxMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Payload);
        var root = json.RootElement;
        var messageId = TryGetGuid(root, "messageId") ?? message.Id;
        const string consumer = "payment-status-consumer";
        if (await _inbox.HasProcessedAsync(messageId, consumer, ct))
            return;

        var orderId = TryGetLong(root, "orderId");
        var paymentId = TryGetLong(root, "paymentId");
        var statusRaw = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
        if (!Enum.TryParse<PaymentTransactionStatus>(statusRaw, true, out var status))
            status = PaymentTransactionStatus.Pending;

        if (paymentId.HasValue)
        {
            var payment = await _paymentRepository.GetByIdAsync(PaymentId.From(paymentId.Value), ct);
            if (payment is not null && status != PaymentTransactionStatus.Pending)
            {
                payment.UpdateProviderState(status, payment.TransactionId, new JsonBlob(message.Payload));
                await _paymentRepository.UpdateAsync(payment, ct);
            }
        }

        if (orderId.HasValue && status != PaymentTransactionStatus.Pending)
        {
            var order = await _orderRepository.GetByIdAsync(OrderId.From(orderId.Value), ct);
            if (order is not null)
            {
                var oldStatus = order.Status;
                _ = _paymentStateApplier.TryApply(order, status, out _);
                await _orderRepository.UpdateAsync(order, ct);
                await _historyWriter.WriteIfChangedAsync(
                    order,
                    oldStatus,
                    Guid.Empty,
                    "outbox",
                    correlationId: messageId.ToString("N"),
                    ct: ct);
                await _cacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);
            }
        }

        await _inbox.MarkProcessedAsync(messageId, consumer, $"event={message.EventType}", ct);
    }

    private async Task ProcessOrderInvalidationAsync(OutboxMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Payload);
        var root = json.RootElement;
        var messageId = TryGetGuid(root, "messageId") ?? message.Id;
        const string consumer = "order-invalidation-consumer";
        if (await _inbox.HasProcessedAsync(messageId, consumer, ct))
            return;

        var orderId = TryGetLong(root, "orderId");
        if (orderId.HasValue)
        {
            var order = await _orderRepository.GetByIdAsync(OrderId.From(orderId.Value), ct);
            if (order is not null)
                await _cacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);
        }

        await _inbox.MarkProcessedAsync(messageId, consumer, $"event={message.EventType}", ct);
    }

    private async Task MarkInventoryEventAsSeenAsync(OutboxMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Payload);
        var messageId = json.RootElement.TryGetProperty("messageId", out var idProp) && Guid.TryParse(idProp.GetString(), out var parsed)
            ? parsed
            : message.Id;
        const string consumer = "inventory-event-consumer";
        if (await _inbox.HasProcessedAsync(messageId, consumer, ct))
            return;

        await _inbox.MarkProcessedAsync(messageId, consumer, $"event={message.EventType}", ct);
    }

    private static Guid? TryGetGuid(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        return Guid.TryParse(prop.GetString(), out var value) ? value : null;
    }

    private static long? TryGetLong(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var numeric))
            return numeric;
        return long.TryParse(prop.GetString(), out var parsed) ? parsed : null;
    }
}
