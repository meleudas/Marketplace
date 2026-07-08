using Marketplace.Application.Products.DTOs;

namespace Marketplace.Application.Products.Catalog;

public interface ICatalogFacetReadService
{
    Task<CatalogProductFacetsDto> GetFacetsAsync(
        IReadOnlyList<long>? categoryIds = null,
        Guid? companyId = null,
        CancellationToken ct = default);
}
