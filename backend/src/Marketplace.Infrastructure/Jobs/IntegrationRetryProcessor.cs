using System.Text.Json;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class IntegrationRetryProcessor
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly ICheckoutInventoryService _checkoutInventory;
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IInventoryReservationReleaseService _releaseService;
    private readonly IAppNotificationRedispatcher _notificationRedispatcher;
    private readonly IntegrationRetryOptions _retryOptions;

    public IntegrationRetryProcessor(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILiqPayPort liqPayPort,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderPaymentStateApplier paymentStateApplier,
        IOrderStatusHistoryWriter historyWriter,
        ICheckoutInventoryService checkoutInventory,
        IInventoryReservationRepository reservationRepository,
        IInventoryReservationReleaseService releaseService,
        IAppNotificationRedispatcher notificationRedispatcher,
        IOptions<IntegrationRetryOptions> retryOptions)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
        _orderMutationCoordinator = orderMutationCoordinator;
        _paymentStateApplier = paymentStateApplier;
        _historyWriter = historyWriter;
        _checkoutInventory = checkoutInventory;
        _reservationRepository = reservationRepository;
        _releaseService = releaseService;
        _notificationRedispatcher = notificationRedispatcher;
        _retryOptions = retryOptions.Value;
    }

    public async Task<bool> TryProcessAsync(IntegrationRetryEntry entry, CancellationToken ct)
    {
        return entry.Kind switch
        {
            IntegrationRetryKinds.PaymentSync => await ProcessPaymentSyncAsync(entry, ct),
            IntegrationRetryKinds.InventoryExpire => await ProcessInventoryExpireAsync(entry, ct),
            IntegrationRetryKinds.NotificationDispatch => await ProcessNotificationDispatchAsync(entry, ct),
            _ => throw new InvalidOperationException($"Unsupported integration retry kind: {entry.Kind}")
        };
    }

    private async Task<bool> ProcessPaymentSyncAsync(IntegrationRetryEntry entry, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(entry.PayloadJson);
        if (!json.RootElement.TryGetProperty("paymentId", out var paymentIdProp) || !paymentIdProp.TryGetInt64(out var paymentId))
            return true;

        var payment = await _paymentRepository.GetByIdAsync(PaymentId.From(paymentId), ct);
        if (payment is null || string.IsNullOrWhiteSpace(payment.TransactionId))
            return true;

        var statusResult = await _liqPayPort.GetPaymentStatusAsync(payment.TransactionId!, ct);
        if (!statusResult.IsSuccess)
            throw new InvalidOperationException(statusResult.Error ?? "Failed to sync payment status");

        var mapped = MapStatus(statusResult.Status);
        if (mapped == PaymentTransactionStatus.Pending)
            return true;

        payment.UpdateProviderState(mapped, statusResult.TransactionId, new JsonBlob(statusResult.RawResponse));
        await _paymentRepository.UpdateAsync(payment, ct);

        var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is not null)
        {
            var oldStatus = order.Status;
            _paymentStateApplier.TryApply(order, mapped, out _);
            await _orderRepository.UpdateAsync(order, ct);
            await _historyWriter.WriteIfChangedAsync(order, oldStatus, Guid.Empty, "integration-retry", correlationId: statusResult.TransactionId, ct: ct);
            await _orderMutationCoordinator.PublishPaymentStatusChangedAsync(
                payment.Id.Value,
                order.Id.Value,
                order.CustomerId,
                order.CompanyId.Value,
                mapped.ToString(),
                "integration-retry",
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

        return true;
    }

    private async Task<bool> ProcessInventoryExpireAsync(IntegrationRetryEntry entry, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(entry.PayloadJson);
        if (!json.RootElement.TryGetProperty("reservationId", out var reservationIdProp) || !reservationIdProp.TryGetInt64(out var reservationId))
            return true;

        var reservation = await _reservationRepository.GetByIdAsync(InventoryReservationId.From(reservationId), ct);
        if (reservation is null)
            return true;

        await _releaseService.ReleaseAsync(reservation, null, "expired", expired: true, ct);
        return true;
    }

    private Task<bool> ProcessNotificationDispatchAsync(IntegrationRetryEntry entry, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(entry.PayloadJson);
        var root = json.RootElement;
        if (!root.TryGetProperty("templateKey", out var templateKeyProp))
            return Task.FromResult(true);

        var templateKey = templateKeyProp.GetString();
        if (string.IsNullOrWhiteSpace(templateKey))
            return Task.FromResult(true);

        if (!root.TryGetProperty("correlationId", out var correlationIdProp)
            || !correlationIdProp.TryGetGuid(out var correlationId))
            return Task.FromResult(true);

        var channels = root.TryGetProperty("channels", out var channelsProp) && channelsProp.TryGetInt32(out var c)
            ? c
            : 0;
        var audience = root.TryGetProperty("audience", out var audienceProp) && audienceProp.TryGetInt32(out var a)
            ? a
            : 0;
        Guid? targetUserId = root.TryGetProperty("targetUserId", out var userProp)
            && userProp.ValueKind != JsonValueKind.Null
            && userProp.TryGetGuid(out var uid)
            ? uid
            : null;
        Guid? targetCompanyId = root.TryGetProperty("targetCompanyId", out var companyProp)
            && companyProp.ValueKind != JsonValueKind.Null
            && companyProp.TryGetGuid(out var cid)
            ? cid
            : null;
        var payloadJson = root.TryGetProperty("payloadJson", out var payloadProp)
            ? payloadProp.GetString()
            : "{}";

        if (entry.Attempts >= _retryOptions.MaxAttempts)
        {
            MarketplaceMetrics.NotificationDispatchDeadLetter.Add(1,
                new KeyValuePair<string, object?>("template_key", templateKey));
            throw new InvalidOperationException("Notification dispatch retry exhausted");
        }

        _notificationRedispatcher.EnqueueDispatch(
            templateKey,
            correlationId,
            channels,
            audience,
            targetUserId,
            targetCompanyId,
            payloadJson);
        return Task.FromResult(true);
    }

    private static PaymentTransactionStatus MapStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "success" => PaymentTransactionStatus.Completed,
            "reversed" => PaymentTransactionStatus.Refunded,
            "refunded" => PaymentTransactionStatus.Refunded,
            "failure" => PaymentTransactionStatus.Failed,
            "error" => PaymentTransactionStatus.Failed,
            _ => PaymentTransactionStatus.Pending
        };
}
