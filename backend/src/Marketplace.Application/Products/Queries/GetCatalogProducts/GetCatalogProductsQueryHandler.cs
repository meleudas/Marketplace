using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Products.Queries.GetCatalogProducts;

public sealed class GetCatalogProductsQueryHandler : IRequestHandler<GetCatalogProductsQuery, Result<IReadOnlyList<ProductListItemDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetCatalogProductsQueryHandler(IProductRepository productRepository, IWarehouseStockRepository stockRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _productRepository = productRepository;
        _stockRepository = stockRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<IReadOnlyList<ProductListItemDto>>> Handle(GetCatalogProductsQuery request, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync<List<ProductListItemDto>>(CatalogCacheKeys.ProductList, ct);
            if (cached is not null)
                return Result<IReadOnlyList<ProductListItemDto>>.Success(cached);

            var products = await _productRepository.ListActiveAsync(ct);
            var dtos = new List<ProductListItemDto>(products.Count);
            foreach (var p in products)
            {
                var stockRows = await _stockRepository.ListByProductAsync(p.CompanyId, p.Id, ct);
                var available = stockRows.Sum(x => x.Available);
                var status = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
                dtos.Add(ProductMapper.ToListItemDto(p, available, status));
            }

            await _cache.SetAsync(CatalogCacheKeys.ProductList, dtos, _ttl.CatalogProductList, ct);
            return Result<IReadOnlyList<ProductListItemDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ProductListItemDto>>.Failure($"Failed to get catalog products: {ex.Message}");
        }
    }
}
