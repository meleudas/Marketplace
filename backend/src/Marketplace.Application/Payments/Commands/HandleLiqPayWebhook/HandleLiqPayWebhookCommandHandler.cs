using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Orders.Services;
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
    private readonly IOrderCacheInvalidationService _orderCacheInvalidation;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly IOutboxWriter _outbox;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly IInboxDeduplicator _inbox;
    private readonly IAppNotificationScheduler _appNotifications;

    public HandleLiqPayWebhookCommandHandler(
        ILiqPayPort liqPayPort,
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IOrderCacheInvalidationService orderCacheInvalidation,
        IOrderPaymentStateApplier paymentStateApplier,
        IOutboxWriter outbox,
        IOrderStatusHistoryWriter historyWriter,
        IInboxDeduplicator inbox,
        IAppNotificationScheduler appNotifications)
    {
        _liqPayPort = liqPayPort;
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _orderCacheInvalidation = orderCacheInvalidation;
        _paymentStateApplier = paymentStateApplier;
        _outbox = outbox;
        _historyWriter = historyWriter;
        _inbox = inbox;
        _appNotifications = appNotifications;
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
                return Result.Success(); // idempotent duplicate webhook

            payment.UpdateProviderState(mappedStatus, transactionId, new JsonBlob(json.GetRawText()));
            await _paymentRepository.UpdateAsync(payment, ct);
            await _outbox.AppendAsync(
                "Payment",
                payment.Id.Value.ToString(),
                "PaymentStatusChanged",
                JsonSerializer.Serialize(new
                {
                    messageId = Guid.NewGuid(),
                    paymentId = payment.Id.Value,
                    orderId = payment.OrderId.Value,
                    transactionId,
                    status = mappedStatus.ToString(),
                    source = "webhook"
                }),
                ct);

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
                await _orderCacheInvalidation.InvalidateOrderAsync(order.Id.Value, order.CustomerId, order.CompanyId.Value, ct);

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
}
