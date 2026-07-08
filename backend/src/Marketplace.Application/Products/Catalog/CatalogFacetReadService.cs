using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Products.Catalog;

public sealed class CatalogFacetReadService : ICatalogFacetReadService
{
    private readonly IProductFacetSourceRepository _facetSourceRepository;
    private readonly CatalogFacetAggregator _aggregator;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public CatalogFacetReadService(
        IProductFacetSourceRepository facetSourceRepository,
        CatalogFacetAggregator aggregator,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl)
    {
        _facetSourceRepository = facetSourceRepository;
        _aggregator = aggregator;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<CatalogProductFacetsDto> GetFacetsAsync(
        IReadOnlyList<long>? categoryIds = null,
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var cacheKey = CatalogCacheKeys.ProductFacets(categoryIds, companyId);
        var cached = await _cache.GetAsync<CatalogProductFacetsDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var sources = await _facetSourceRepository.ListActiveFacetSourcesAsync(categoryIds, companyId, ct);
        var facets = _aggregator.Aggregate(sources);
        await _cache.SetAsync(cacheKey, facets, _ttl.CatalogProductFacets, ct);
        return facets;
    }
}
