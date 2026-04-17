using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.SearchCatalogProducts;

public sealed class SearchCatalogProductsQueryHandler : IRequestHandler<SearchCatalogProductsQuery, Result<ProductSearchResultDto>>
{
    private readonly IProductSearchService _searchService;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseStockRepository _stockRepository;

    public SearchCatalogProductsQueryHandler(
        IProductSearchService searchService,
        IProductRepository productRepository,
        IWarehouseStockRepository stockRepository)
    {
        _searchService = searchService;
        _productRepository = productRepository;
        _stockRepository = stockRepository;
    }

    public async Task<Result<ProductSearchResultDto>> Handle(SearchCatalogProductsQuery request, CancellationToken ct)
    {
        var searchTerm = string.IsNullOrWhiteSpace(request.Name) ? request.Query : request.Name;
        try
        {
            var esResult = await _searchService.SearchCatalogProductsAsync(
                searchTerm,
                request.CategoryIds,
                request.CompanyId,
                request.MinPrice,
                request.MaxPrice,
                request.AvailabilityStatus,
                request.Sort,
                request.Page,
                request.PageSize,
                ct);

            if (esResult.IsSuccess)
                return esResult;
        }
        catch
        {
            // fall through to DB fallback
        }

        try
        {
            var products = await _productRepository.ListActiveAsync(ct);

            IEnumerable<(ProductListItemDto Dto, int Score)> rows = [];
            foreach (var p in products)
            {
                var stockRows = await _stockRepository.ListByProductAsync(p.CompanyId, p.Id, ct);
                var available = stockRows.Sum(x => x.Available);
                var availability = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
                var dto = ProductMapper.ToListItemDto(p, available, availability);

                if (request.CategoryIds is { Count: > 0 } && !request.CategoryIds.Contains(dto.CategoryId))
                    continue;
                if (request.CompanyId.HasValue && dto.CompanyId != request.CompanyId.Value)
                    continue;
                if (request.MinPrice.HasValue && dto.Price < request.MinPrice.Value)
                    continue;
                if (request.MaxPrice.HasValue && dto.Price > request.MaxPrice.Value)
                    continue;
                if (!string.IsNullOrWhiteSpace(request.AvailabilityStatus) &&
                    !string.Equals(dto.AvailabilityStatus, request.AvailabilityStatus.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                var score = Score(dto, searchTerm);
                if (!string.IsNullOrWhiteSpace(searchTerm) && score == 0)
                    continue;

                rows = rows.Append((dto, score));
            }

            var sorted = Sort(rows, request.Sort).ToList();
            var total = sorted.Count;
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
            var items = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Dto)
                .ToList();

            return Result<ProductSearchResultDto>.Success(new ProductSearchResultDto(items, total, page, pageSize));
        }
        catch (Exception ex)
        {
            return Result<ProductSearchResultDto>.Failure($"Failed to search products: {ex.Message}");
        }
    }

    private static int Score(ProductListItemDto dto, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 1;

        var q = query.Trim().ToLowerInvariant();
        var score = 0;
        if (dto.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 3;
        if (dto.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 2;
        if (dto.Slug.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 1;
        return score;
    }

    private static IEnumerable<(ProductListItemDto Dto, int Score)> Sort(
        IEnumerable<(ProductListItemDto Dto, int Score)> rows,
        string? sort)
    {
        return (sort ?? "relevance").Trim().ToLowerInvariant() switch
        {
            "price_asc" => rows.OrderBy(x => x.Dto.Price),
            "price_desc" => rows.OrderByDescending(x => x.Dto.Price),
            "newest" => rows.OrderByDescending(x => x.Dto.CreatedAt),
            _ => rows.OrderByDescending(x => x.Score).ThenByDescending(x => x.Dto.CreatedAt)
        };
    }
}
