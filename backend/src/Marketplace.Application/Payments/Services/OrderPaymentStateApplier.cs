using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Payments.Enums;

namespace Marketplace.Application.Payments.Services;

public sealed class OrderPaymentStateApplier : IOrderPaymentStateApplier
{
    public bool TryApply(Order order, PaymentTransactionStatus status, out string? reason)
    {
        try
        {
            switch (status)
            {
                case PaymentTransactionStatus.Completed:
                    order.MarkPaid();
                    break;
                case PaymentTransactionStatus.Refunded:
                    order.MarkRefunded();
                    break;
                case PaymentTransactionStatus.Failed:
                    order.MarkFailed();
                    break;
                default:
                    reason = "Unsupported payment status transition";
                    return false;
            }

            reason = null;
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }
}
