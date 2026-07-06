namespace Marketplace.Application.Products.Ports;

public sealed record CatalogOnSaleProductFilters(
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinDiscountPercent,
    string? AvailabilityStatus,
    string? Sort,
    int Page,
    int PageSize,
    string? SearchAfter);
