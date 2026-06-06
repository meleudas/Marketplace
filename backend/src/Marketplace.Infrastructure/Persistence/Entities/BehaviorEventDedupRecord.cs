namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class BehaviorEventDedupRecord
{
    public long Id { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public short EventType { get; set; }
    public DateTime BucketStartedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}
