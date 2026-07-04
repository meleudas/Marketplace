namespace Marketplace.Application.Products.Ports;

public sealed record CatalogProductSearchFilters(
    string? Name,
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    string? Author,
    string? Format,
    string? Genre,
    IReadOnlyList<string>? Tags,
    string? Sort,
    int Page,
    int PageSize,
    string? SearchAfter);
