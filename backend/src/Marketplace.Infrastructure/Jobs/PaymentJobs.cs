using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;

namespace Marketplace.Infrastructure.Jobs;

public sealed class PaymentJobs
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILiqPayPort _liqPayPort;

    public PaymentJobs(IPaymentRepository paymentRepository, IOrderRepository orderRepository, ILiqPayPort liqPayPort)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _liqPayPort = liqPayPort;
    }

    public async Task SyncPendingPaymentsAsync(CancellationToken ct = default)
    {
        var pending = await _paymentRepository.ListByStatusAsync(PaymentTransactionStatus.Pending, ct);
        foreach (var payment in pending.Where(x => !string.IsNullOrWhiteSpace(x.TransactionId)))
        {
            var statusResult = await _liqPayPort.GetPaymentStatusAsync(payment.TransactionId!, ct);
            if (!statusResult.IsSuccess)
                continue;

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

            if (mapped == PaymentTransactionStatus.Completed)
                order.MarkPaid();
            else if (mapped == PaymentTransactionStatus.Refunded)
                order.MarkRefunded();
            else if (mapped == PaymentTransactionStatus.Failed)
                order.MarkFailed();

            await _orderRepository.UpdateAsync(order, ct);
        }
    }
}
