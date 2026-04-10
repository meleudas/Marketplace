using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Catalog.Entities;

public sealed class ProductImage : AuditableSoftDeleteAggregateRoot<ProductImageId>
{
    private ProductImage() { }

    public ProductId ProductId { get; private set; } = null!;
    public string ImageUrl { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsMain { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public long? FileSize { get; private set; }

    public static ProductImage Create(
        ProductImageId id,
        ProductId productId,
        string imageUrl,
        string thumbnailUrl,
        string altText,
        int sortOrder,
        bool isMain,
        int? width,
        int? height,
        long? fileSize)
    {
        var now = DateTime.UtcNow;
        return new ProductImage
        {
            Id = id,
            ProductId = productId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            AltText = altText,
            SortOrder = sortOrder,
            IsMain = isMain,
            Width = width,
            Height = height,
            FileSize = fileSize,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static ProductImage Reconstitute(
        ProductImageId id,
        ProductId productId,
        string imageUrl,
        string thumbnailUrl,
        string altText,
        int sortOrder,
        bool isMain,
        int? width,
        int? height,
        long? fileSize,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ProductId = productId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            AltText = altText,
            SortOrder = sortOrder,
            IsMain = isMain,
            Width = width,
            Height = height,
            FileSize = fileSize,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void Update(
        string imageUrl,
        string thumbnailUrl,
        string altText,
        int sortOrder,
        bool isMain,
        int? width,
        int? height,
        long? fileSize)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted product image.");

        ImageUrl = imageUrl;
        ThumbnailUrl = thumbnailUrl;
        AltText = altText;
        SortOrder = sortOrder;
        IsMain = isMain;
        Width = width;
        Height = height;
        FileSize = fileSize;
        Touch();
    }
}
