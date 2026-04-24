using Marketplace.Domain.Common.Exceptions;
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
    public decimal? Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public long ViewCount { get; private set; }
    public long SalesCount { get; private set; }
    public bool HasVariants { get; private set; }

    public static Product Create(
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
        bool hasVariants)
    {
        ValidateText(name, "Product name");
        ValidateText(slug, "Product slug");
        ValidateText(description, "Product description");
        ValidateStock(stock, minStock);

        var now = DateTime.UtcNow;
        return new Product
        {
            Id = id,
            CompanyId = companyId,
            Name = name.Trim(),
            Slug = slug.Trim(),
            Description = description.Trim(),
            Price = price,
            OldPrice = oldPrice,
            Stock = stock,
            MinStock = minStock,
            CategoryId = categoryId,
            Status = ProductStatus.Draft,
            Rating = null,
            ReviewCount = 0,
            ViewCount = 0,
            SalesCount = 0,
            HasVariants = hasVariants,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

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

    public void UpdateProfile(
        string name,
        string slug,
        string description,
        Money price,
        Money? oldPrice,
        int minStock,
        CategoryId categoryId,
        bool hasVariants)
    {
        EnsureNotDeleted();
        ValidateText(name, "Product name");
        ValidateText(slug, "Product slug");
        ValidateText(description, "Product description");
        ValidateStock(Stock, minStock);

        Name = name.Trim();
        Slug = slug.Trim();
        Description = description.Trim();
        Price = price;
        OldPrice = oldPrice;
        MinStock = minStock;
        CategoryId = categoryId;
        HasVariants = hasVariants;
        Touch();
    }

    public void Activate()
    {
        EnsureNotDeleted();
        Status = ProductStatus.Active;
        Touch();
    }

    public void Archive()
    {
        EnsureNotDeleted();
        Status = ProductStatus.Archived;
        Touch();
    }

    public void SetDerivedStock(int stock, int minStock)
    {
        EnsureNotDeleted();
        ValidateStock(stock, minStock);
        Stock = stock;
        MinStock = minStock;
        Touch();
    }

    public void SetReviewStats(decimal? rating, int reviewCount)
    {
        EnsureNotDeleted();
        if (reviewCount < 0)
            throw new DomainException("Review count cannot be negative");
        Rating = rating;
        ReviewCount = reviewCount;
        Touch();
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        MarkDeleted();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted product");
    }

    private static void ValidateText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} cannot be empty");
    }

    private static void ValidateStock(int stock, int minStock)
    {
        if (stock < 0)
            throw new DomainException("Product stock cannot be negative");
        if (minStock < 0)
            throw new DomainException("Product minStock cannot be negative");
    }
}
