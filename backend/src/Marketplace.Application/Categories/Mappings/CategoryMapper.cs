using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Categories.Entities;

namespace Marketplace.Application.Categories.Mappings;

public static class CategoryMapper
{
    public static CategoryDto ToDto(Category category) =>
        new(
            category.Id.Value,
            category.Name,
            category.Slug,
            category.ImageUrl,
            category.ParentId?.Value,
            category.Description,
            category.Meta.Raw,
            category.SortOrder,
            category.IsActive,
            category.ProductCount,
            category.CreatedAt,
            category.UpdatedAt,
            category.IsDeleted,
            category.DeletedAt);
}
