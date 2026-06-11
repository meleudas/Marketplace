namespace Marketplace.Domain.Support.Enums;

public enum SupportTicketStatus : short
{
    Open = 0,
    Assigned = 1,
    PendingCustomer = 2,
    Resolved = 3,
    Closed = 4,
    Escalated = 5
}
