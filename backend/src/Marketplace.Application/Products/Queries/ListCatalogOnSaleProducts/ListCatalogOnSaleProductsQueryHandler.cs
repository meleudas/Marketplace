using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Marketplace.Application.Products.Queries.ListCatalogOnSaleProducts;

public sealed class ListCatalogOnSaleProductsQueryHandler
    : IRequestHandler<ListCatalogOnSaleProductsQuery, Result<ProductSearchResultDto>>
{
    private readonly IProductSearchService _searchService;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly ILogger<ListCatalogOnSaleProductsQueryHandler> _logger;

    public ListCatalogOnSaleProductsQueryHandler(
        IProductSearchService searchService,
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IWarehouseStockRepository stockRepository,
        ILogger<ListCatalogOnSaleProductsQueryHandler> logger)
    {
        _searchService = searchService;
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<Result<ProductSearchResultDto>> Handle(
        ListCatalogOnSaleProductsQuery request,
        CancellationToken ct)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 200);
        var filters = new CatalogOnSaleProductFilters(
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.MinDiscountPercent,
            request.AvailabilityStatus,
            request.Sort,
            page,
            pageSize,
            request.SearchAfter);

        try
        {
            var esResult = await _searchService.SearchCatalogOnSaleProductsAsync(filters, ct);
            if (esResult.IsSuccess)
                return Result<ProductSearchResultDto>.Success(await EnrichAsync(esResult.Value!, ct));

            _logger.LogInformation(
                "Catalog on-sale fallback to DB because Elasticsearch returned failure: {Error}",
                esResult.Error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog on-sale fallback to DB because Elasticsearch query threw");
        }

        try
        {
            var products = await _productRepository.ListActiveOnSaleAsync(
                request.CompanyId,
                request.CategoryIds,
                request.MinPrice,
                request.MaxPrice,
                request.MinDiscountPercent,
                ct);

            var rows = new List<ProductListItemDto>(products.Count);
            foreach (var product in products)
            {
                var stockRows = await _stockRepository.ListByProductAsync(product.CompanyId, product.Id, ct);
                var available = stockRows.Sum(x => x.Available);
                var availability = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
                var dto = ProductMapper.ToListItemDto(product, available, availability);

                if (!string.IsNullOrWhiteSpace(request.AvailabilityStatus) &&
                    !string.Equals(dto.AvailabilityStatus, request.AvailabilityStatus.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                rows.Add(dto);
            }

            var sorted = Sort(rows, request.Sort).ToList();
            var total = sorted.Count;
            var items = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Result<ProductSearchResultDto>.Success(
                await EnrichAsync(new ProductSearchResultDto(items, total, page, pageSize), ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog on-sale DB fallback failed");
            return Result<ProductSearchResultDto>.Failure($"Failed to list on-sale products: {ex.Message}");
        }
    }

    private async Task<ProductSearchResultDto> EnrichAsync(ProductSearchResultDto result, CancellationToken ct)
    {
        var items = await ProductListImageEnricher.WithImageUrlsAsync(result.Items, _productImageRepository, ct);
        return result with { Items = items };
    }

    private static IEnumerable<ProductListItemDto> Sort(IEnumerable<ProductListItemDto> rows, string? sort)
    {
        return (sort ?? "discount_desc").Trim().ToLowerInvariant() switch
        {
            "price_asc" => rows.OrderBy(x => x.Price),
            "price_desc" => rows.OrderByDescending(x => x.Price),
            "newest" => rows.OrderByDescending(x => x.CreatedAt),
            _ => rows.OrderByDescending(x => x.DiscountPercent ?? 0m).ThenByDescending(x => x.CreatedAt)
        };
    }
}
