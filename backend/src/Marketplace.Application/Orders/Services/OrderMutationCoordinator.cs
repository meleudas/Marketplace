using System.Text.Json;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Application.Orders.Services;

public sealed class OrderMutationCoordinator
{
    private readonly IOrderCacheInvalidationService _cacheInvalidation;
    private readonly IOutboxWriter _outbox;

    public OrderMutationCoordinator(IOrderCacheInvalidationService cacheInvalidation, IOutboxWriter outbox)
    {
        _cacheInvalidation = cacheInvalidation;
        _outbox = outbox;
    }

    public async Task PublishOrderChangedAsync(Order order, string eventType, string correlationKey, CancellationToken ct = default)
    {
        await _cacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);
        await _outbox.AppendAsync(
            "Order",
            order.Id.Value.ToString(),
            eventType,
            JsonSerializer.Serialize(new
            {
                messageId = DomainEventIds.ForOrderEvent(order.Id.Value, eventType, correlationKey),
                orderId = order.Id.Value,
                customerId = order.CustomerId,
                companyId = order.CompanyId.Value,
                status = order.Status.ToString()
            }),
            ct);
    }

    public async Task PublishPaymentStatusChangedAsync(
        long paymentId,
        long orderId,
        Guid customerId,
        Guid companyId,
        string status,
        string source,
        string? transactionId,
        CancellationToken ct = default)
    {
        await _cacheInvalidation.InvalidateOrderAsync(orderId, customerId, companyId, ct);
        await _outbox.AppendAsync(
            "Payment",
            paymentId.ToString(),
            "PaymentStatusChanged",
            JsonSerializer.Serialize(new
            {
                messageId = DomainEventIds.ForPaymentStatus(paymentId, status, source),
                paymentId,
                orderId,
                transactionId,
                status,
                source
            }),
            ct);
    }
}
