using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Catalog.Entities;

public sealed class Product : AuditableSoftDeleteAggregateRoot<ProductId>
{
    private Product() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero;
    public Money? OldPrice { get; private set; }
    public int Stock { get; private set; }
    public int MinStock { get; private set; }
    public CategoryId CategoryId { get; private set; } = null!;
    public ProductStatus Status { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public decimal? Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public long ViewCount { get; private set; }
    public long SalesCount { get; private set; }
    public bool HasVariants { get; private set; }

    public static Product Reconstitute(
        ProductId id,
        CompanyId companyId,
        string name,
        string slug,
        string description,
        Money price,
        Money? oldPrice,
        int stock,
        int minStock,
        CategoryId categoryId,
        ProductStatus status,
        DateTime? approvedAt,
        string? approvedByUserId,
        decimal? rating,
        int reviewCount,
        long viewCount,
        long salesCount,
        bool hasVariants,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            Name = name,
            Slug = slug,
            Description = description,
            Price = price,
            OldPrice = oldPrice,
            Stock = stock,
            MinStock = minStock,
            CategoryId = categoryId,
            Status = status,
            ApprovedAt = approvedAt,
            ApprovedByUserId = approvedByUserId,
            Rating = rating,
            ReviewCount = reviewCount,
            ViewCount = viewCount,
            SalesCount = salesCount,
            HasVariants = hasVariants,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
