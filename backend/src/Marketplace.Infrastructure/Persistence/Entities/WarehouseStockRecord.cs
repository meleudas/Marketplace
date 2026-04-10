namespace Marketplace.Infrastructure.Persistence.Entities;

public class WarehouseStockRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long ProductId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int ReorderPoint { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
