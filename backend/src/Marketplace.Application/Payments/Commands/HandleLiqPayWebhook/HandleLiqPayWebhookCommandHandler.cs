using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Services;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;

public sealed class HandleLiqPayWebhookCommandHandler : IRequestHandler<HandleLiqPayWebhookCommand, Result>
{
    private readonly ILiqPayPort _liqPayPort;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly IInboxDeduplicator _inbox;
    private readonly IAppNotificationScheduler _appNotifications;
    private readonly ICheckoutInventoryService _checkoutInventory;
    private readonly IOrderFinancialsWriter _orderFinancialsWriter;
    private readonly IOutboxWriter _outbox;

    public HandleLiqPayWebhookCommandHandler(
        ILiqPayPort liqPayPort,
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderPaymentStateApplier paymentStateApplier,
        IOrderStatusHistoryWriter historyWriter,
        IInboxDeduplicator inbox,
        IAppNotificationScheduler appNotifications,
        ICheckoutInventoryService checkoutInventory,
        IOrderFinancialsWriter orderFinancialsWriter,
        IOutboxWriter? outbox = null)
    {
        _liqPayPort = liqPayPort;
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _orderMutationCoordinator = orderMutationCoordinator;
        _paymentStateApplier = paymentStateApplier;
        _historyWriter = historyWriter;
        _inbox = inbox;
        _appNotifications = appNotifications;
        _checkoutInventory = checkoutInventory;
        _orderFinancialsWriter = orderFinancialsWriter;
        _outbox = outbox ?? NoOpOutboxWriter.Instance;
    }

    public async Task<Result> Handle(HandleLiqPayWebhookCommand request, CancellationToken ct)
    {
        using var activity = MarketplaceTelemetry.StartActivity("payment.webhook.liqpay");
        activity?.SetTag("provider", "liqpay");
        try
        {
            var isValid = await _liqPayPort.VerifySignatureAsync(request.Data, request.Signature, ct);
            if (!isValid)
                return Result.Failure("Invalid LiqPay signature");

            var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(request.Data))).RootElement;
            var transactionId = json.TryGetProperty("order_id", out var orderIdProp) ? orderIdProp.GetString() : null;
            var statusRaw = json.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(transactionId))
                return Result.Failure("Transaction id is missing");

            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId, ct);
            if (payment is null)
                return Result.Failure("Payment not found");

            var mappedStatus = MapStatus(statusRaw);
            var messageId = BuildWebhookMessageId(transactionId, statusRaw, request.Signature, request.Data);
            const string consumer = "liqpay-webhook";
            if (await _inbox.HasProcessedAsync(messageId, consumer, ct))
                return Result.Success();

            if (IsStatusDowngrade(payment.Status, mappedStatus))
                return Result.Success();
            if (payment.Status == mappedStatus)
                return Result.Success();

            payment.UpdateProviderState(mappedStatus, transactionId, new JsonBlob(json.GetRawText()));
            await _paymentRepository.UpdateAsync(payment, ct);

            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is not null)
            {
                var oldStatus = order.Status;
                _paymentStateApplier.TryApply(order, mappedStatus, out _);

                await _orderRepository.UpdateAsync(order, ct);
                await _historyWriter.WriteIfChangedAsync(
                    order,
                    oldStatus,
                    Guid.Empty,
                    "webhook",
                    correlationId: transactionId,
                    ct: ct);

                await _orderMutationCoordinator.PublishPaymentStatusChangedAsync(
                    payment.Id.Value,
                    order.Id.Value,
                    order.CustomerId,
                    order.CompanyId.Value,
                    mappedStatus.ToString(),
                    "webhook",
                    transactionId,
                    ct);

                if (mappedStatus == PaymentTransactionStatus.Completed)
                {
                    await _checkoutInventory.ConfirmForOrderAsync(order.Id, order.CompanyId, ct);
                    await _orderFinancialsWriter.PostOnPaymentCompletedAsync(payment.Id, ct);
                    await PublishPurchaseCompletedEventAsync(order, transactionId ?? string.Empty, ct);
                }

                if (mappedStatus == PaymentTransactionStatus.Failed)
                {
                    await _checkoutInventory.ReleaseForOrderAsync(
                        order.Id,
                        order.CompanyId,
                        null,
                        "payment-failed",
                        ct);
                }

                if (mappedStatus is PaymentTransactionStatus.Completed
                    or PaymentTransactionStatus.Failed
                    or PaymentTransactionStatus.Refunded)
                {
                    await _appNotifications.ScheduleAsync(
                        new AppNotificationRequest
                        {
                            TemplateKey = AppNotificationTemplateKeys.UserPaymentStatus,
                            CorrelationId = AppNotificationCorrelationIds.PaymentBuyerNotify(
                                transactionId,
                                mappedStatus.ToString()),
                            Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                            Audience = AppNotificationAudienceKind.User,
                            TargetUserId = order.CustomerId,
                            PayloadJson = JsonSerializer.Serialize(new
                            {
                                orderId = order.Id.Value,
                                orderNumber = order.OrderNumber,
                                paymentStatus = mappedStatus.ToString(),
                                orderStatus = order.Status.ToString()
                            })
                        },
                        ct);
                }
            }

            await _inbox.MarkProcessedAsync(messageId, consumer, $"transactionId={transactionId}", ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to handle LiqPay webhook: {ex.Message}");
        }
    }

    private static PaymentTransactionStatus MapStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "success" => PaymentTransactionStatus.Completed,
            "subscribed" => PaymentTransactionStatus.Completed,
            "reversed" => PaymentTransactionStatus.Refunded,
            "refunded" => PaymentTransactionStatus.Refunded,
            "failure" => PaymentTransactionStatus.Failed,
            "error" => PaymentTransactionStatus.Failed,
            _ => PaymentTransactionStatus.Pending
        };

    private static Guid BuildWebhookMessageId(string transactionId, string? statusRaw, string signature, string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{transactionId}|{statusRaw}|{signature}|{data}"));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, 16);
        return new Guid(guidBytes);
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

    private Task PublishPurchaseCompletedEventAsync(Marketplace.Domain.Orders.Entities.Order order, string transactionId, CancellationToken ct)
    {
        var messageId = DomainEventIds.ForPaymentStatus(order.Id.Value, BehaviorEventType.PurchaseCompleted.ToString(), "liqpay");
        var payload = JsonSerializer.Serialize(new
        {
            messageId,
            eventId = 0,
            eventType = BehaviorEventType.PurchaseCompleted.ToString(),
            occurredAtUtc = DateTime.UtcNow,
            userId = order.CustomerId,
            sessionId = $"user:{order.CustomerId:N}",
            source = "payments:liqpay:webhook",
            schemaVersion = 1,
            eventKey = $"purchase|{order.Id.Value}|{transactionId}",
            payloadJson = JsonSerializer.Serialize(new { orderId = order.Id.Value, transactionId })
        });
        return _outbox.AppendAsync("BehaviorEvent", order.Id.Value.ToString(), "behavior.event.ingested", payload, ct);
    }
}
