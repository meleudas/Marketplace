namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class OrderItemRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtMoment { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
