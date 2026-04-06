namespace Marketplace.Application.Categories.DTOs;

public sealed record CategoryDto(
    long Id,
    string Name,
    string Slug,
    string? ImageUrl,
    long? ParentId,
    string? Description,
    string? MetaRaw,
    int SortOrder,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsDeleted,
    DateTime? DeletedAt);
