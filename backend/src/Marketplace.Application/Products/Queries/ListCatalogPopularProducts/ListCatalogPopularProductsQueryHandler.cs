using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Marketplace.Application.Products.Queries.ListCatalogPopularProducts;

public sealed class ListCatalogPopularProductsQueryHandler
    : IRequestHandler<ListCatalogPopularProductsQuery, Result<ProductSearchResultDto>>
{
    private readonly IProductSearchService _searchService;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ListCatalogPopularProductsQueryHandler> _logger;

    public ListCatalogPopularProductsQueryHandler(
        IProductSearchService searchService,
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IWarehouseStockRepository stockRepository,
        ICategoryRepository categoryRepository,
        ILogger<ListCatalogPopularProductsQueryHandler> logger)
    {
        _searchService = searchService;
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _stockRepository = stockRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<Result<ProductSearchResultDto>> Handle(ListCatalogPopularProductsQuery request, CancellationToken ct)
    {
        var (page, pageSize) = CatalogProductListSupport.NormalizePaging(request.Page, request.PageSize);
        var categoryIds = await CatalogCategoryFilterExpander.ExpandAsync(_categoryRepository, request.CategoryIds, ct);
        var filters = new CatalogBrowsableProductFilters(
            categoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.AvailabilityStatus,
            page,
            pageSize,
            request.SearchAfter);

        try
        {
            var esResult = await _searchService.SearchCatalogPopularProductsAsync(filters, ct);
            if (esResult.IsSuccess)
                return Result<ProductSearchResultDto>.Success(
                    await CatalogProductListSupport.EnrichAsync(esResult.Value!, _productImageRepository, ct));

            _logger.LogInformation(
                "Catalog popular-products fallback to DB because Elasticsearch returned failure: {Error}",
                esResult.Error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog popular-products fallback to DB because Elasticsearch query threw");
        }

        try
        {
            var products = await _productRepository.ListActivePopularAsync(
                request.CompanyId,
                categoryIds,
                request.MinPrice,
                request.MaxPrice,
                ct);

            var rows = await CatalogProductListSupport.BuildListItemsAsync(
                products,
                request.AvailabilityStatus,
                _stockRepository,
                ct);

            var result = CatalogProductListSupport.Paginate(rows, page, pageSize);
            return Result<ProductSearchResultDto>.Success(
                await CatalogProductListSupport.EnrichAsync(result, _productImageRepository, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog popular-products DB fallback failed");
            return Result<ProductSearchResultDto>.Failure($"Failed to list popular products: {ex.Message}");
        }
    }
}
