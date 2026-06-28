using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Products.Ports;

public interface IProductSearchService
{
    Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
        string? name,
        IReadOnlyList<long>? categoryIds,
        Guid? companyId,
        decimal? minPrice,
        decimal? maxPrice,
        string? availabilityStatus,
        string? sort,
        int page,
        int pageSize,
        string? searchAfter,
        CancellationToken ct = default);
}
