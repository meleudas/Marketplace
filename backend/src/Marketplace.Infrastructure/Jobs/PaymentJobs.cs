using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Services;
using Marketplace.Infrastructure.Observability;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;

namespace Marketplace.Infrastructure.Jobs;

public sealed class PaymentJobs
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;
    private readonly IOrderCacheInvalidationService _orderCacheInvalidation;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOutboxWriter _outbox;
    private readonly IOrderStatusHistoryWriter _historyWriter;

    public PaymentJobs(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILiqPayPort liqPayPort,
        IOrderCacheInvalidationService orderCacheInvalidation,
        IOrderPaymentStateApplier paymentStateApplier,
        IOutboxWriter outbox,
        IOrderStatusHistoryWriter historyWriter)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
        _orderCacheInvalidation = orderCacheInvalidation;
        _paymentStateApplier = paymentStateApplier;
        _outbox = outbox;
        _historyWriter = historyWriter;
    }

    public async Task SyncPendingPaymentsAsync(CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.HangfireJobLatencyMs, new KeyValuePair<string, object?>("job", "payments-sync-pending"));
        var pending = await _paymentRepository.ListByStatusAsync(PaymentTransactionStatus.Pending, ct);
        foreach (var payment in pending.Where(x => !string.IsNullOrWhiteSpace(x.TransactionId)))
        {
            var statusResult = await _liqPayPort.GetPaymentStatusAsync(payment.TransactionId!, ct);
            if (!statusResult.IsSuccess)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "payments-sync-pending")]);
                continue;
            }

            var mapped = statusResult.Status?.ToLowerInvariant() switch
            {
                "success" => PaymentTransactionStatus.Completed,
                "reversed" => PaymentTransactionStatus.Refunded,
                "refunded" => PaymentTransactionStatus.Refunded,
                "failure" => PaymentTransactionStatus.Failed,
                "error" => PaymentTransactionStatus.Failed,
                _ => PaymentTransactionStatus.Pending
            };

            if (mapped == PaymentTransactionStatus.Pending)
                continue;

            payment.UpdateProviderState(mapped, statusResult.TransactionId, new Marketplace.Domain.Common.ValueObjects.JsonBlob(statusResult.RawResponse));
            await _paymentRepository.UpdateAsync(payment, ct);
            await _outbox.AppendAsync(
                "Payment",
                payment.Id.Value.ToString(),
                "PaymentStatusChanged",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    messageId = Guid.NewGuid(),
                    paymentId = payment.Id.Value,
                    orderId = payment.OrderId.Value,
                    transactionId = statusResult.TransactionId,
                    status = mapped.ToString(),
                    source = "job"
                }),
                ct);

            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is null)
                continue;

            var oldStatus = order.Status;
            _paymentStateApplier.TryApply(order, mapped, out _);

            await _orderRepository.UpdateAsync(order, ct);
            await _historyWriter.WriteIfChangedAsync(
                order,
                oldStatus,
                Guid.Empty,
                "job",
                correlationId: statusResult.TransactionId,
                ct: ct);
            await _orderCacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);
        }
        MarketplaceMetrics.HangfireJobs.Add(1, [new KeyValuePair<string, object?>("job", "payments-sync-pending"), new KeyValuePair<string, object?>("status", "success")]);
    }
}
