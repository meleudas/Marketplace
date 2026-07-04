using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Products.Ports;

public interface IProductSearchService
{
    Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
        CatalogProductSearchFilters filters,
        CancellationToken ct = default);
}
