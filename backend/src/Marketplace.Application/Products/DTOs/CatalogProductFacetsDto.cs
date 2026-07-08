namespace Marketplace.Application.Products.DTOs;

public sealed record CatalogProductFacetsDto(
    IReadOnlyList<CatalogFacetOptionDto> Authors,
    IReadOnlyList<CatalogFacetOptionDto> Genres,
    IReadOnlyList<CatalogFacetOptionDto> Formats,
    IReadOnlyList<CatalogFacetOptionDto> Tags);
