using Marketplace.Application.Common;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Hangfire;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Marketplace.Infrastructure.Jobs;

public sealed class PaymentJobs
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly ICheckoutInventoryService _checkoutInventory;
    private readonly IIntegrationRetryStore _integrationRetryStore;
    private readonly IntegrationRetryOptions _retryOptions;

    public PaymentJobs(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILiqPayPort liqPayPort,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderPaymentStateApplier paymentStateApplier,
        IOrderStatusHistoryWriter historyWriter,
        ICheckoutInventoryService checkoutInventory,
        IIntegrationRetryStore integrationRetryStore,
        IOptions<IntegrationRetryOptions> retryOptions)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
        _orderMutationCoordinator = orderMutationCoordinator;
        _paymentStateApplier = paymentStateApplier;
        _historyWriter = historyWriter;
        _checkoutInventory = checkoutInventory;
        _integrationRetryStore = integrationRetryStore;
        _retryOptions = retryOptions.Value;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task SyncPendingPaymentsAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("payments-sync-pending-liqpay", SyncPendingPaymentsCoreAsync, ct);

    private async Task SyncPendingPaymentsCoreAsync(CancellationToken ct)
    {
        using var span = MarketplaceTelemetry.StartActivity("payment.sync.pending");
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.HangfireJobLatencyMs, new KeyValuePair<string, object?>("job", "payments-sync-pending"));
        var pending = await _paymentRepository.ListByStatusAsync(PaymentTransactionStatus.Pending, ct);
        foreach (var payment in pending.Where(x => !string.IsNullOrWhiteSpace(x.TransactionId)))
        {
            var statusResult = await _liqPayPort.GetPaymentStatusAsync(payment.TransactionId!, ct);
            if (!statusResult.IsSuccess)
            {
                MarketplaceMetrics.HangfireJobErrors.Add(1, [new KeyValuePair<string, object?>("job", "payments-sync-pending")]);
                var nextAttempt = RetryBackoffCalculator.ComputeNextAttemptUtc(
                    1,
                    _retryOptions.BaseBackoffMinutes,
                    _retryOptions.MaxBackoffMinutes,
                    DateTime.UtcNow);
                await _integrationRetryStore.UpsertAsync(
                    new IntegrationRetryUpsert(
                        IntegrationRetryKinds.PaymentSync,
                        "Payment",
                        payment.Id.Value.ToString(),
                        JsonSerializer.Serialize(new { paymentId = payment.Id.Value }),
                        statusResult.Error ?? "Failed to sync payment status"),
                    nextAttempt,
                    ct);
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

            await _orderMutationCoordinator.PublishPaymentStatusChangedAsync(
                payment.Id.Value,
                order.Id.Value,
                order.CustomerId,
                order.CompanyId.Value,
                mapped.ToString(),
                "job",
                statusResult.TransactionId,
                ct);

            if (mapped == PaymentTransactionStatus.Failed)
            {
                await _checkoutInventory.ReleaseForOrderAsync(
                    order.Id,
                    order.CompanyId,
                    null,
                    "payment-failed",
                    ct);
            }
        }
        MarketplaceMetrics.HangfireJobs.Add(1, [new KeyValuePair<string, object?>("job", "payments-sync-pending"), new KeyValuePair<string, object?>("status", "success")]);
    }
}
