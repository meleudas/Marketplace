namespace Marketplace.Domain.Returns.Enums;

public enum ReturnRequestStatus : short
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    Received = 3,
    Refunded = 4,
    Closed = 5
}
