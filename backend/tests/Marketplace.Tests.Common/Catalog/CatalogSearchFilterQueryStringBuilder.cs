using Marketplace.Application.Products.Queries.SearchCatalogProducts;

namespace Marketplace.Tests.Common.Catalog;

public static class CatalogSearchFilterQueryStringBuilder
{
    public static string Build(SearchCatalogProductsQuery query)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Name))
            parts.Add($"name={Uri.EscapeDataString(query.Name)}");

        if (!string.IsNullOrWhiteSpace(query.Query))
            parts.Add($"query={Uri.EscapeDataString(query.Query)}");

        if (query.CategoryIds is { Count: > 0 })
        {
            foreach (var categoryId in query.CategoryIds)
                parts.Add($"categoryIds={categoryId}");
        }

        if (query.CompanyId.HasValue)
            parts.Add($"companyId={query.CompanyId.Value}");

        if (query.MinPrice.HasValue)
            parts.Add($"minPrice={query.MinPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        if (query.MaxPrice.HasValue)
            parts.Add($"maxPrice={query.MaxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(query.AvailabilityStatus))
            parts.Add($"availabilityStatus={Uri.EscapeDataString(query.AvailabilityStatus)}");

        if (query.Authors is { Count: > 0 })
        {
            foreach (var author in query.Authors)
                parts.Add($"authors={Uri.EscapeDataString(author)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Format))
            parts.Add($"format={Uri.EscapeDataString(query.Format)}");

        if (query.Genres is { Count: > 0 })
        {
            foreach (var genre in query.Genres)
                parts.Add($"genres={Uri.EscapeDataString(genre)}");
        }

        if (query.Tags is { Count: > 0 })
        {
            foreach (var tag in query.Tags)
                parts.Add($"tags={Uri.EscapeDataString(tag)}");
        }

        parts.Add($"page={query.Page}");
        parts.Add($"pageSize={query.PageSize}");

        return string.Join('&', parts);
    }
}
