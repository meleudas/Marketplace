namespace Marketplace.Domain.Payments.Enums;

public enum RefundStatus : short
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3
}
