using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.SyncPaymentStatus;

public sealed class SyncPaymentStatusCommandHandler : IRequestHandler<SyncPaymentStatusCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;

    public SyncPaymentStatusCommandHandler(IPaymentRepository paymentRepository, IOrderRepository orderRepository, ILiqPayPort liqPayPort)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
    }

    public async Task<Result> Handle(SyncPaymentStatusCommand request, CancellationToken ct)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(PaymentId.From(request.PaymentId), ct);
            if (payment is null)
                return Result.Failure("Payment not found");
            if (string.IsNullOrWhiteSpace(payment.TransactionId))
                return Result.Failure("Transaction id is missing");

            var statusResult = await _liqPayPort.GetPaymentStatusAsync(payment.TransactionId, ct);
            if (!statusResult.IsSuccess)
                return Result.Failure(statusResult.Error ?? "Failed to sync payment status");

            var mapped = statusResult.Status?.ToLowerInvariant() switch
            {
                "success" => PaymentTransactionStatus.Completed,
                "reversed" => PaymentTransactionStatus.Refunded,
                "refunded" => PaymentTransactionStatus.Refunded,
                "failure" => PaymentTransactionStatus.Failed,
                "error" => PaymentTransactionStatus.Failed,
                _ => PaymentTransactionStatus.Pending
            };

            payment.UpdateProviderState(mapped, statusResult.TransactionId, new JsonBlob(statusResult.RawResponse));
            await _paymentRepository.UpdateAsync(payment, ct);

            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is not null)
            {
                if (mapped == PaymentTransactionStatus.Completed)
                    order.MarkPaid();
                else if (mapped == PaymentTransactionStatus.Refunded)
                    order.MarkRefunded();
                else if (mapped == PaymentTransactionStatus.Failed)
                    order.MarkFailed();
                await _orderRepository.UpdateAsync(order, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to sync payment: {ex.Message}");
        }
    }
}
