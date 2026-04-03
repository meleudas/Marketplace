using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Catalog.Entities;

public sealed class ProductDetail : AuditableSoftDeleteAggregateRoot<ProductDetailId>
{
    private ProductDetail() { }

    public ProductId ProductId { get; private set; } = null!;
    public string Slug { get; private set; } = string.Empty;
    public JsonBlob Attributes { get; private set; } = JsonBlob.Empty;
    public JsonBlob Variants { get; private set; } = JsonBlob.Empty;
    public JsonBlob Specifications { get; private set; } = JsonBlob.Empty;
    public JsonBlob Seo { get; private set; } = JsonBlob.Empty;
    public JsonBlob ContentBlocks { get; private set; } = JsonBlob.Empty;
    public IReadOnlyList<string> Tags { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Brands { get; private set; } = Array.Empty<string>();

    public static ProductDetail Reconstitute(
        ProductDetailId id,
        ProductId productId,
        string slug,
        JsonBlob attributes,
        JsonBlob variants,
        JsonBlob specifications,
        JsonBlob seo,
        JsonBlob contentBlocks,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> brands,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ProductId = productId,
            Slug = slug,
            Attributes = attributes,
            Variants = variants,
            Specifications = specifications,
            Seo = seo,
            ContentBlocks = contentBlocks,
            Tags = tags,
            Brands = brands,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
