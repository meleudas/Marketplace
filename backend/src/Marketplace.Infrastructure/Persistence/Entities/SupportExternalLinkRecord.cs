namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SupportExternalLinkRecord
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalTicketId { get; set; } = string.Empty;
    public short SyncState { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime? ExternalUpdatedAt { get; set; }
    public long ExternalSequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
