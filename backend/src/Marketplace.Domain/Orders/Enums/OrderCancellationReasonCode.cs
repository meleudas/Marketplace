namespace Marketplace.Domain.Orders.Enums;

public enum OrderCancellationReasonCode : short
{
    ChangedMind = 1,
    WrongAddress = 2,
    DuplicateOrder = 3,
    PaymentIssue = 4,
    OutOfStock = 5,
    CustomerRequest = 6,
    FraudSuspected = 7,
    Other = 99
}
