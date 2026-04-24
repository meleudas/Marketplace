using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.RequestRefund;

public sealed class RequestRefundCommandHandler : IRequestHandler<RequestRefundCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;

    public RequestRefundCommandHandler(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IOrderRepository orderRepository,
        ILiqPayPort liqPayPort)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
    }

    public async Task<Result> Handle(RequestRefundCommand request, CancellationToken ct)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(PaymentId.From(request.PaymentId), ct);
            if (payment is null)
                return Result.Failure("Payment not found");
            if (payment.Status != PaymentTransactionStatus.Completed)
                return Result.Failure("Refund is allowed only for completed payment");
            if (string.IsNullOrWhiteSpace(payment.TransactionId))
                return Result.Failure("Payment transaction id is missing");

            var response = await _liqPayPort.RefundAsync(
                new LiqPayRefundRequest(payment.TransactionId, request.Amount, payment.Currency, request.Reason),
                ct);
            if (!response.IsSuccess)
                return Result.Failure(response.Error ?? "LiqPay refund failed");

            var refund = Refund.Create(
                RefundId.From(0),
                payment.Id,
                payment.OrderId,
                new Money(request.Amount),
                request.Reason,
                request.AdminUserId);
            refund.SetStatus(RefundStatus.Completed);
            await _refundRepository.AddAsync(refund, ct);

            payment.UpdateProviderState(PaymentTransactionStatus.Refunded, response.TransactionId, new JsonBlob(response.RawResponse));
            await _paymentRepository.UpdateAsync(payment, ct);

            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is not null)
            {
                order.MarkRefunded();
                await _orderRepository.UpdateAsync(order, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to request refund: {ex.Message}");
        }
    }
}
