using Marketplace.Application.Products.Catalog;

namespace Marketplace.Application.Products.Queries.SearchCatalogProducts;

internal static class CatalogSearchFilterDetection
{
    public static bool HasActiveCatalogFilters(SearchCatalogProductsQuery request) =>
        !string.IsNullOrWhiteSpace(request.Name)
        || !string.IsNullOrWhiteSpace(request.Query)
        || request.CategoryIds is { Count: > 0 }
        || request.CompanyId.HasValue
        || request.MinPrice.HasValue
        || request.MaxPrice.HasValue
        || !string.IsNullOrWhiteSpace(request.AvailabilityStatus)
        || ProductCatalogFacetReader.HasFacetFilters(
            request.Authors,
            request.Format,
            request.Genres,
            request.Tags);
}
