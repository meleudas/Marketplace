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

namespace Marketplace.Application.Products.Queries.GetCatalogProductBySlug;

public sealed class GetCatalogProductBySlugQueryHandler : IRequestHandler<GetCatalogProductBySlugQuery, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductDetailRepository _detailRepository;
    private readonly IProductImageRepository _imageRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetCatalogProductBySlugQueryHandler(
        IProductRepository productRepository,
        IProductDetailRepository detailRepository,
        IProductImageRepository imageRepository,
        IWarehouseStockRepository stockRepository,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl)
    {
        _productRepository = productRepository;
        _detailRepository = detailRepository;
        _imageRepository = imageRepository;
        _stockRepository = stockRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<ProductDto>> Handle(GetCatalogProductBySlugQuery request, CancellationToken ct)
    {
        try
        {
            var cacheKey = CatalogCacheKeys.ProductDetailPrefix + request.Slug;
            var cached = await _cache.GetAsync<ProductDto>(cacheKey, ct);
            if (cached is not null)
                return Result<ProductDto>.Success(cached);

            var product = await _productRepository.GetBySlugAsync(request.Slug, ct);
            if (product is null || product.Status != Domain.Catalog.Enums.ProductStatus.Active)
                return Result<ProductDto>.Failure("Product not found");

            var detail = await _detailRepository.GetByProductIdAsync(product.Id, ct);
            var images = await _imageRepository.ListByProductIdAsync(product.Id, ct);
            var stockRows = await _stockRepository.ListByProductAsync(product.CompanyId, product.Id, ct);
            var available = stockRows.Sum(x => x.Available);
            var status = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";

            var dto = new ProductDto(
                ProductMapper.ToListItemDto(product, available, status),
                detail is null ? null : ProductMapper.ToDetailDto(detail),
                images.Select(ProductMapper.ToImageDto).ToList());

            await _cache.SetAsync(cacheKey, dto, _ttl.CatalogProductDetail, ct);
            return Result<ProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure($"Failed to get product: {ex.Message}");
        }
    }
}
