namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ShipmentItemRecord
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public long OrderItemId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
