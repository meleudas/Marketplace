namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SupportTicketEventRecord
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public short EventType { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
