using System.Text;
using System.Text.Json;
using Marketplace.Application.Payments.Ports;
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

    public HandleLiqPayWebhookCommandHandler(
        ILiqPayPort liqPayPort,
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository)
    {
        _liqPayPort = liqPayPort;
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(HandleLiqPayWebhookCommand request, CancellationToken ct)
    {
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
            if (payment.Status == mappedStatus)
                return Result.Success(); // idempotent duplicate webhook

            payment.UpdateProviderState(mappedStatus, transactionId, new JsonBlob(json.GetRawText()));
            await _paymentRepository.UpdateAsync(payment, ct);

            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is not null)
            {
                if (mappedStatus == PaymentTransactionStatus.Completed)
                    order.MarkPaid();
                else if (mappedStatus == PaymentTransactionStatus.Refunded)
                    order.MarkRefunded();
                else if (mappedStatus == PaymentTransactionStatus.Failed)
                    order.MarkFailed();

                await _orderRepository.UpdateAsync(order, ct);
            }

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
}
