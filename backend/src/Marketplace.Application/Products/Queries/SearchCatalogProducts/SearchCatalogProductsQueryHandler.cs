using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Marketplace.Application.Products.Queries.SearchCatalogProducts;

public sealed class SearchCatalogProductsQueryHandler : IRequestHandler<SearchCatalogProductsQuery, Result<ProductSearchResultDto>>
{
    private readonly IProductSearchService _searchService;
    private readonly IProductRepository _productRepository;
    private readonly IProductDetailRepository _productDetailRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly ILogger<SearchCatalogProductsQueryHandler> _logger;

    public SearchCatalogProductsQueryHandler(
        IProductSearchService searchService,
        IProductRepository productRepository,
        IProductDetailRepository productDetailRepository,
        IProductImageRepository productImageRepository,
        IWarehouseStockRepository stockRepository,
        ILogger<SearchCatalogProductsQueryHandler> logger)
    {
        _searchService = searchService;
        _productRepository = productRepository;
        _productDetailRepository = productDetailRepository;
        _productImageRepository = productImageRepository;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<Result<ProductSearchResultDto>> Handle(SearchCatalogProductsQuery request, CancellationToken ct)
    {
        var searchTerm = string.IsNullOrWhiteSpace(request.Name) ? request.Query : request.Name;
        var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);
        var hasFacetFilters = ProductCatalogFacetReader.HasFacetFilters(
            request.Authors,
            request.Format,
            request.Genres,
            request.Tags);
        var filters = new CatalogProductSearchFilters(
            searchTerm,
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.AvailabilityStatus,
            request.Authors,
            request.Format,
            request.Genres,
            request.Tags,
            request.Sort,
            request.Page,
            request.PageSize,
            request.SearchAfter);

        try
        {
            var esResult = await _searchService.SearchCatalogProductsAsync(filters, ct);
            if (esResult.IsSuccess)
            {
                if (esResult.Value!.Total > 0 || (!hasFacetFilters && !hasSearchTerm))
                    return Result<ProductSearchResultDto>.Success(await EnrichAsync(esResult.Value, ct));

                _logger.LogInformation(
                    "Catalog search fallback to DB because Elasticsearch returned zero results with filters/search term.");
            }
            else
            {
                _logger.LogInformation(
                    "Catalog search fallback to DB because Elasticsearch returned failure: {Error}",
                    esResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog search fallback to DB because Elasticsearch query threw");
        }

        try
        {
            return Result<ProductSearchResultDto>.Success(await SearchInDatabaseAsync(request, searchTerm, hasFacetFilters, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog DB fallback search failed");
            return Result<ProductSearchResultDto>.Failure($"Failed to search products: {ex.Message}");
        }
    }

    private async Task<ProductSearchResultDto> SearchInDatabaseAsync(
        SearchCatalogProductsQuery request,
        string? searchTerm,
        bool hasFacetFilters,
        CancellationToken ct)
    {
        var products = await _productRepository.ListActiveAsync(ct);
        var rows = new List<(ProductListItemDto Dto, int Score)>(products.Count);

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

            if (hasFacetFilters)
            {
                var detail = await _productDetailRepository.GetByProductIdAsync(p.Id, ct);
                var facets = ProductCatalogFacetReader.Read(
                    detail?.Attributes ?? JsonBlob.Empty,
                    detail?.Tags ?? [],
                    detail?.Brands ?? []);
                if (!ProductCatalogFacetReader.Matches(facets, request.Authors, request.Format, request.Genres, request.Tags))
                    continue;
            }

            var score = Score(dto, searchTerm);
            if (!string.IsNullOrWhiteSpace(searchTerm) && score == 0)
                continue;

            rows.Add((dto, score));
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

        return await EnrichAsync(new ProductSearchResultDto(items, total, page, pageSize), ct);
    }

    private async Task<ProductSearchResultDto> EnrichAsync(ProductSearchResultDto result, CancellationToken ct)
    {
        var items = await ProductListImageEnricher.WithImageUrlsAsync(result.Items, _productImageRepository, ct);
        return result with { Items = items };
    }

    private static int Score(ProductListItemDto dto, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 1;

        var q = query.Trim();
        var score = 0;
        if (dto.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 3;
        else if (NameFuzzyMatches(dto.Name, q))
            score += 2;
        if (dto.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 2;
        if (dto.Slug.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 1;
        return score;
    }

    private static bool NameFuzzyMatches(string name, string query)
    {
        var normalizedName = NormalizeForFuzzy(name);
        var normalizedQuery = NormalizeForFuzzy(query);
        if (normalizedName.Length == 0 || normalizedQuery.Length == 0)
            return false;

        if (normalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
            return true;

        var queryDistance = AllowedDistance(normalizedQuery.Length);
        if (LevenshteinDistance(normalizedName, normalizedQuery, queryDistance) <= queryDistance)
            return true;

        var nameTokens = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in nameTokens)
        {
            var tokenDistance = AllowedDistance(Math.Max(token.Length, normalizedQuery.Length));
            if (LevenshteinDistance(token, normalizedQuery, tokenDistance) <= tokenDistance)
                return true;
        }

        return false;
    }

    private static string NormalizeForFuzzy(string value)
        => new(value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ')
            .ToArray());

    private static int AllowedDistance(int length)
    {
        if (length <= 4)
            return 1;
        if (length <= 8)
            return 2;
        return 3;
    }

    private static int LevenshteinDistance(string left, string right, int maxDistance)
    {
        if (Math.Abs(left.Length - right.Length) > maxDistance)
            return maxDistance + 1;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            var rowMin = current[0];

            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
                rowMin = Math.Min(rowMin, current[j]);
            }

            if (rowMin > maxDistance)
                return maxDistance + 1;

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
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
