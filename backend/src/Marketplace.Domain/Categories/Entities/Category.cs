using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Categories.Entities;

public sealed class Category : AuditableSoftDeleteAggregateRoot<CategoryId>
{
    private Category() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public CategoryId? ParentId { get; private set; }
    public string? Description { get; private set; }
    public JsonBlob Meta { get; private set; } = JsonBlob.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public int ProductCount { get; private set; }

    public static Category Create(
        CategoryId id,
        string name,
        string slug,
        string? imageUrl,
        CategoryId? parentId,
        string? description,
        JsonBlob? meta,
        int sortOrder,
        bool isActive = true)
    {
        ValidateName(name);
        ValidateSlug(slug);
        ValidateSortOrder(sortOrder);

        var now = DateTime.UtcNow;
        return new Category
        {
            Id = id,
            Name = name.Trim(),
            Slug = slug.Trim(),
            ImageUrl = imageUrl,
            ParentId = parentId,
            Description = description,
            Meta = meta ?? JsonBlob.Empty,
            SortOrder = sortOrder,
            IsActive = isActive,
            ProductCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public static Category Reconstitute(
        CategoryId id,
        string name,
        string slug,
        string? imageUrl,
        CategoryId? parentId,
        string? description,
        JsonBlob meta,
        int sortOrder,
        bool isActive,
        int productCount,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Slug = slug,
            ImageUrl = imageUrl,
            ParentId = parentId,
            Description = description,
            Meta = meta,
            SortOrder = sortOrder,
            IsActive = isActive,
            ProductCount = productCount,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void UpdateDetails(
        string name,
        string slug,
        string? imageUrl,
        CategoryId? parentId,
        string? description,
        JsonBlob? meta,
        int sortOrder)
    {
        EnsureNotDeleted();
        ValidateName(name);
        ValidateSlug(slug);
        ValidateSortOrder(sortOrder);

        Name = name.Trim();
        Slug = slug.Trim();
        ImageUrl = imageUrl;
        ParentId = parentId;
        Description = description;
        Meta = meta ?? JsonBlob.Empty;
        SortOrder = sortOrder;
        Touch();
    }

    public void Activate()
    {
        EnsureNotDeleted();
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        IsActive = false;
        Touch();
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        MarkDeleted();
    }

    public void SetProductCount(int productCount)
    {
        EnsureNotDeleted();
        if (productCount < 0)
            throw new DomainException("Category productCount cannot be negative");

        ProductCount = productCount;
        Touch();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted category");
    }

    private static void ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Category name cannot be empty");
    }

    private static void ValidateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Category slug cannot be empty");
    }

    private static void ValidateSortOrder(int value)
    {
        if (value < 0)
            throw new DomainException("Category sortOrder cannot be negative");
    }
}
