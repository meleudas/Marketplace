using Marketplace.Application.Finance.Services;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Payments.Services;

public sealed record PaymentRefundRequest(
    long PaymentId,
    decimal? Amount,
    string Reason,
    Guid ProcessedByUserId);

public sealed record PaymentRefundResult(long RefundId, bool OrderMarkedRefunded);

public interface IPaymentRefundExecutor
{
    Task<Result<PaymentRefundResult>> ExecuteAsync(PaymentRefundRequest request, CancellationToken ct = default);
}

public sealed class PaymentRefundExecutor : IPaymentRefundExecutor
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly IOrderPaymentStateApplier _paymentStateApplier;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderFinancialsWriter _orderFinancialsWriter;

    public PaymentRefundExecutor(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IOrderRepository orderRepository,
        ILiqPayPort liqPayPort,
        IOrderStatusHistoryWriter historyWriter,
        IOrderPaymentStateApplier paymentStateApplier,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderFinancialsWriter orderFinancialsWriter)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
        _historyWriter = historyWriter;
        _paymentStateApplier = paymentStateApplier;
        _orderMutationCoordinator = orderMutationCoordinator;
        _orderFinancialsWriter = orderFinancialsWriter;
    }

    public async Task<Result<PaymentRefundResult>> ExecuteAsync(PaymentRefundRequest request, CancellationToken ct = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(PaymentId.From(request.PaymentId), ct);
        if (payment is null)
            return Result<PaymentRefundResult>.Failure("Payment not found");
        if (payment.Status != PaymentTransactionStatus.Completed)
            return Result<PaymentRefundResult>.Failure("Refund is allowed only for completed payment");

        var existingRefunds = await _refundRepository.ListByOrderIdAsync(payment.OrderId, ct);
        var refundedTotal = existingRefunds.Where(x => x.Status == RefundStatus.Completed).Sum(x => x.Amount.Amount);
        var refundAmount = request.Amount ?? payment.Amount.Amount;
        if (refundAmount <= 0 || refundedTotal + refundAmount > payment.Amount.Amount)
            return Result<PaymentRefundResult>.Failure("Invalid refund amount");

        if (string.IsNullOrWhiteSpace(payment.TransactionId))
            return Result<PaymentRefundResult>.Failure("Payment has no transaction id");

        var liqPayResult = await _liqPayPort.RefundAsync(
            new LiqPayRefundRequest(payment.TransactionId, refundAmount, payment.Currency, request.Reason),
            ct);
        if (!liqPayResult.IsSuccess)
            return Result<PaymentRefundResult>.Failure(liqPayResult.Error ?? "Refund failed");

        var refund = Refund.Create(
            RefundId.From(0),
            payment.Id,
            payment.OrderId,
            new Money(refundAmount),
            request.Reason,
            request.ProcessedByUserId);
        refund.SetStatus(RefundStatus.Completed);
        var savedRefund = await _refundRepository.AddAsync(refund, ct);

        await _orderFinancialsWriter.PostRefundReversalAsync(payment.Id, refundAmount, request.Reason, ct);

        payment.UpdateProviderState(PaymentTransactionStatus.Refunded, payment.TransactionId, new JsonBlob(liqPayResult.RawResponse));
        await _paymentRepository.UpdateAsync(payment, ct);

        var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
        var orderMarkedRefunded = false;
        if (order is not null)
        {
            var oldStatus = order.Status;
            if (_paymentStateApplier.TryApply(order, PaymentTransactionStatus.Refunded, out _))
            {
                orderMarkedRefunded = true;
                await _orderRepository.UpdateAsync(order, ct);
                await _historyWriter.WriteIfChangedAsync(order, oldStatus, request.ProcessedByUserId, "refund", comment: request.Reason, ct: ct);
                await _orderMutationCoordinator.PublishPaymentStatusChangedAsync(
                    payment.Id.Value,
                    order.Id.Value,
                    order.CustomerId,
                    order.CompanyId.Value,
                    PaymentTransactionStatus.Refunded.ToString(),
                    "refund",
                    payment.TransactionId,
                    ct);
            }
        }

        return Result<PaymentRefundResult>.Success(new PaymentRefundResult(savedRefund.Id.Value, orderMarkedRefunded));
    }
}
