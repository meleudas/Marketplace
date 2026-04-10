namespace Marketplace.Infrastructure.Persistence.Entities;

public class InventoryReservationRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long ProductId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public short Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
