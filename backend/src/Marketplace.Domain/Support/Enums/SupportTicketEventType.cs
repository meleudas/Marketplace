namespace Marketplace.Domain.Support.Enums;

public enum SupportTicketEventType : short
{
    Created = 0,
    MessageAdded = 1,
    Assigned = 2,
    StatusChanged = 3,
    Escalated = 4,
    HelpdeskSynced = 5
}
