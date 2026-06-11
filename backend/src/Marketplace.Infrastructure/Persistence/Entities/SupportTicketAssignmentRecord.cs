namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SupportTicketAssignmentRecord
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string AssigneeUserId { get; set; } = string.Empty;
    public string AssignedByUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
