namespace Marketplace.Infrastructure.Persistence.Entities;

public class ProductImageRecord
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string OriginalObjectKey { get; set; } = string.Empty;
    public string ImageObjectKey { get; set; } = string.Empty;
    public string ThumbnailObjectKey { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
