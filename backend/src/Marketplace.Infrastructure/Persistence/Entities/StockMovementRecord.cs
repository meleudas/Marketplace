namespace Marketplace.Infrastructure.Persistence.Entities;

public class StockMovementRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long ProductId { get; set; }
    public short Type { get; set; }
    public int Quantity { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Reason { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
