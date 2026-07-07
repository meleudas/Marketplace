namespace Marketplace.Application.Products.Ports;

public sealed record CatalogProductSearchFilters(
    string? Name,
    IReadOnlyList<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    IReadOnlyList<string>? Authors,
    string? Format,
    IReadOnlyList<string>? Genres,
    IReadOnlyList<string>? Tags,
    string? Sort,
    int Page,
    int PageSize,
    string? SearchAfter);
