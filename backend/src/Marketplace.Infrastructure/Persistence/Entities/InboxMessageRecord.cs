namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class InboxMessageRecord
{
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
    public string? Metadata { get; set; }
}
