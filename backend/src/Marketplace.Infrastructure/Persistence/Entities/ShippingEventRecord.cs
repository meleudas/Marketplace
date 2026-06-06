namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ShippingEventRecord
{
    public long Id { get; set; }
    public short CarrierCode { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
