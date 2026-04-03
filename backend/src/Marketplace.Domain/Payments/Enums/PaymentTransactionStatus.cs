namespace Marketplace.Domain.Payments.Enums;

public enum PaymentTransactionStatus : short
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}
