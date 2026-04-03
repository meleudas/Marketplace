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
}
