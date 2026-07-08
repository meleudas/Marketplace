namespace Marketplace.Application.Products.Ports;

public interface IProductFacetSourceRepository
{
    Task<IReadOnlyList<ProductFacetSourceRow>> ListActiveFacetSourcesAsync(
        IReadOnlyList<long>? categoryIds = null,
        Guid? companyId = null,
        CancellationToken ct = default);
}
