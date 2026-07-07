using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Inventory.Repositories;

namespace Marketplace.Application.Products.Catalog;

public static class CatalogProductListSupport
{
    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);
        return (normalizedPage, normalizedPageSize);
    }

    public static async Task<IReadOnlyList<ProductListItemDto>> BuildListItemsAsync(
        IReadOnlyList<Product> products,
        string? availabilityStatus,
        IWarehouseStockRepository stockRepository,
        CancellationToken ct)
    {
        var rows = new List<ProductListItemDto>(products.Count);
        foreach (var product in products)
        {
            var stockRows = await stockRepository.ListByProductAsync(product.CompanyId, product.Id, ct);
            var available = stockRows.Sum(x => x.Available);
            var availability = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
            var dto = ProductMapper.ToListItemDto(product, available, availability);

            if (!string.IsNullOrWhiteSpace(availabilityStatus) &&
                !string.Equals(dto.AvailabilityStatus, availabilityStatus.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            rows.Add(dto);
        }

        return rows;
    }

    public static async Task<ProductSearchResultDto> EnrichAsync(
        ProductSearchResultDto result,
        IProductImageRepository imageRepository,
        CancellationToken ct)
    {
        var items = await ProductListImageEnricher.WithImageUrlsAsync(result.Items, imageRepository, ct);
        return result with { Items = items };
    }

    public static ProductSearchResultDto Paginate(
        IReadOnlyList<ProductListItemDto> rows,
        int page,
        int pageSize)
    {
        var total = rows.Count;
        var items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new ProductSearchResultDto(items, total, page, pageSize);
    }
}
