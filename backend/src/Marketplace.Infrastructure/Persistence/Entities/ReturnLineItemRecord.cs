namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReturnLineItemRecord
{
    public long Id { get; set; }
    public long ReturnRequestId { get; set; }
    public long OrderItemId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
