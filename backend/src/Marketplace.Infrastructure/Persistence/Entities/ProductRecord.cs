namespace Marketplace.Infrastructure.Persistence.Entities;

public class ProductRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public long CategoryId { get; set; }
    public short Status { get; set; }
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }
    public long ViewCount { get; set; }
    public long SalesCount { get; set; }
    public bool HasVariants { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
