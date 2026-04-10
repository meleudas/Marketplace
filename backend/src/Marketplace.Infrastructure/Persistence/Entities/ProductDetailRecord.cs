namespace Marketplace.Infrastructure.Persistence.Entities;

public class ProductDetailRecord
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? AttributesRaw { get; set; }
    public string? VariantsRaw { get; set; }
    public string? SpecificationsRaw { get; set; }
    public string? SeoRaw { get; set; }
    public string? ContentBlocksRaw { get; set; }
    public string[] Tags { get; set; } = [];
    public string[] Brands { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
