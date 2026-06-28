namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class BehaviorEventRawRecord
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public short EventType { get; set; }
    public short EventVersion { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string Source { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
