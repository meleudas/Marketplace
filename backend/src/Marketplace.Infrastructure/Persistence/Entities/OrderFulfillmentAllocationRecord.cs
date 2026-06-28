namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class OrderFulfillmentAllocationRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long OrderItemId { get; set; }
    public Guid CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public long? ReservationId { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
