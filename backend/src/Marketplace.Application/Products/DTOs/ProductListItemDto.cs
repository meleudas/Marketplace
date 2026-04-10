namespace Marketplace.Application.Products.DTOs;

public sealed record ProductListItemDto(
    long Id,
    Guid CompanyId,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? OldPrice,
    long CategoryId,
    string Status,
    bool HasVariants,
    int Stock,
    int MinStock,
    int AvailableQty,
    string AvailabilityStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt);
