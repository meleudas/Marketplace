namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ShipmentRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public int ShipmentNumber { get; set; }
    public long ShippingMethodId { get; set; }
    public long? WarehouseId { get; set; }
    public short CarrierCode { get; set; }
    public short Status { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public string RawPayload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
