using System.Text.Json;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Behavior.Ports;
using Marketplace.Infrastructure.External.Analytics;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Support.Ports;
using Marketplace.Domain.Catalog.Repositories;
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
    private readonly ISupportHelpdeskSyncHandler _supportHelpdeskSync;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IAnalyticsWarehouseWriter _analyticsWarehouseWriter;

    public OutboxEventProcessor(
        IInboxDeduplicator inbox,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IOrderPaymentStateApplier paymentStateApplier,
        IOrderCacheInvalidationService cacheInvalidation,
        IOrderStatusHistoryWriter historyWriter,
        ISupportHelpdeskSyncHandler supportHelpdeskSync,
        IProductRepository productRepository,
        IAppCachePort cache,
        IAnalyticsWarehouseWriter? analyticsWarehouseWriter = null)
    {
        _inbox = inbox;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _paymentStateApplier = paymentStateApplier;
        _cacheInvalidation = cacheInvalidation;
        _historyWriter = historyWriter;
        _supportHelpdeskSync = supportHelpdeskSync;
        _productRepository = productRepository;
        _cache = cache;
        _analyticsWarehouseWriter = analyticsWarehouseWriter ?? NoOpAnalyticsWarehouseWriter.Instance;
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
                await ProcessInventoryEventAsync(message, ct);
                break;
            case "SupportTicketCreated":
            case "SupportTicketMessageAdded":
            case "SupportTicketStatusChanged":
                await _supportHelpdeskSync.ProcessAsync(message, ct);
                break;
            case "behavior.event.ingested":
                await ProcessBehaviorEventAsync(message, ct);
                break;
            default:
                throw new PermanentOutboxException($"Unsupported outbox event type: {message.EventType}");
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
                if (payment.Status != status && !IsStatusDowngrade(payment.Status, status))
                {
                    payment.UpdateProviderState(status, payment.TransactionId, new JsonBlob(message.Payload));
                    await _paymentRepository.UpdateAsync(payment, ct);
                }
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

    private async Task ProcessInventoryEventAsync(OutboxMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Payload);
        var root = json.RootElement;
        var messageId = TryGetGuid(root, "messageId") ?? message.Id;
        const string consumer = "inventory-event-consumer";
        if (await _inbox.HasProcessedAsync(messageId, consumer, ct))
            return;

        var productId = TryGetLong(root, "productId");
        if (productId.HasValue)
        {
            var product = await _productRepository.GetByIdAsync(ProductId.From(productId.Value), ct);
            if (product is not null)
            {
                await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);
            }
        }

        await _inbox.MarkProcessedAsync(messageId, consumer, $"event={message.EventType}", ct);
    }

    private async Task ProcessBehaviorEventAsync(OutboxMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Payload);
        var root = json.RootElement;
        var messageId = TryGetGuid(root, "messageId") ?? message.Id;
        const string consumer = "behavior-warehouse-consumer";
        if (await _inbox.HasProcessedAsync(messageId, consumer, ct))
            return;

        var occurredAtUtc = TryGetDateTime(root, "occurredAtUtc") ?? message.OccurredAtUtc;
        var userId = TryGetGuid(root, "userId");
        var sessionId = TryGetString(root, "sessionId") ?? "unknown";
        var source = TryGetString(root, "source") ?? "unknown";
        var eventType = TryGetString(root, "eventType") ?? "Unknown";
        var schemaVersion = TryGetShort(root, "schemaVersion") ?? (short)1;
        var payloadJson = TryGetString(root, "payloadJson") ?? "{}";
        _ = TryGetLong(root, "eventId");

        long? productId = null;
        string? query = null;
        try
        {
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            var payloadRoot = payloadDoc.RootElement;
            productId = TryGetLong(payloadRoot, "productId");
            query = TryGetString(payloadRoot, "query");
        }
        catch (JsonException)
        {
            // Keep payload as-is if shape is unexpected.
        }

        await _analyticsWarehouseWriter.WriteEventAsync(
            new AnalyticsWarehouseEvent(
                messageId,
                eventType,
                occurredAtUtc,
                userId,
                sessionId,
                productId,
                query,
                source,
                schemaVersion,
                payloadJson,
                DateTime.UtcNow),
            ct);

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

    private static short? TryGetShort(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt16(out var numeric))
            return numeric;
        return short.TryParse(prop.GetString(), out var parsed) ? parsed : null;
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Null)
            return null;
        return prop.GetString();
    }

    private static DateTime? TryGetDateTime(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static bool IsStatusDowngrade(PaymentTransactionStatus current, PaymentTransactionStatus next)
        => Rank(next) < Rank(current);

    private static int Rank(PaymentTransactionStatus status)
        => status switch
        {
            PaymentTransactionStatus.Pending => 0,
            PaymentTransactionStatus.Failed => 1,
            PaymentTransactionStatus.Completed => 2,
            PaymentTransactionStatus.Refunded => 3,
            _ => 0
        };
}
