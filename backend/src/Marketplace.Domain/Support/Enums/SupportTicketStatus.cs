namespace Marketplace.Domain.Support.Enums;

public enum SupportTicketStatus : short
{
    Open = 0,
    InProgress = 1,
    WaitingCustomer = 2,
    Resolved = 3,
    Closed = 4
}
