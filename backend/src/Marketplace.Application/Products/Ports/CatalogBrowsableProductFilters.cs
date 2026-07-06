namespace Marketplace.Application.Products.Ports;

public sealed record CatalogBrowsableProductFilters(
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    int Page,
    int PageSize,
    string? SearchAfter);
