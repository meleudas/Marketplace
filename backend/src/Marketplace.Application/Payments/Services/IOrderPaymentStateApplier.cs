using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Payments.Enums;

namespace Marketplace.Application.Payments.Services;

public interface IOrderPaymentStateApplier
{
    bool TryApply(Order order, PaymentTransactionStatus status, out string? reason);
}
