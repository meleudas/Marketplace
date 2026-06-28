namespace Marketplace.Infrastructure.External.Search.Documents;

public sealed class ProductSearchDocument
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public long CategoryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasVariants { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public int AvailableQty { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
    public IReadOnlyList<string> Brands { get; set; } = [];
}
