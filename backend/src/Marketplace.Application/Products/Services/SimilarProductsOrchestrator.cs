using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Products.Services;

public sealed class SimilarProductsOrchestrator
{
    private readonly IProductRepository _productRepository;
    private readonly IProductDetailRepository _detailRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IProductSimilarityService _similarityService;
    private readonly IAppCachePort _cache;
    private readonly SimilarProductsOptions _similarOptions;
    private readonly CacheTtlOptions _ttl;
    private readonly ILogger<SimilarProductsOrchestrator> _logger;

    public SimilarProductsOrchestrator(
        IProductRepository productRepository,
        IProductDetailRepository detailRepository,
        IWarehouseStockRepository stockRepository,
        IProductSimilarityService similarityService,
        IAppCachePort cache,
        IOptions<SimilarProductsOptions> similarOptions,
        IOptions<CacheTtlOptions> ttl,
        ILogger<SimilarProductsOrchestrator> logger)
    {
        _productRepository = productRepository;
        _detailRepository = detailRepository;
        _stockRepository = stockRepository;
        _similarityService = similarityService;
        _cache = cache;
        _similarOptions = similarOptions.Value;
        _ttl = ttl.Value;
        _logger = logger;
    }

    public Task<Result<SimilarProductsResultDto>> GetBySlugAsync(string slug, int limit, CancellationToken ct)
        => ResolveAsync(null, slug, limit, ct);

    public Task<Result<SimilarProductsResultDto>> GetByIdAsync(long productId, int limit, CancellationToken ct)
        => ResolveAsync(productId, null, limit, ct);

    private async Task<Result<SimilarProductsResultDto>> ResolveAsync(long? productId, string? slug, int limit, CancellationToken ct)
    {
        try
        {
            var product = productId.HasValue
                ? await _productRepository.GetByIdAsync(ProductId.From(productId.Value), ct)
                : await _productRepository.GetBySlugAsync(slug!, ct);

            if (product is null || product.IsDeleted || product.Status != ProductStatus.Active)
                return Result<SimilarProductsResultDto>.Failure("Product not found");

            var effectiveLimit = Math.Clamp(
                limit <= 0 ? _similarOptions.DefaultLimit : limit,
                1,
                Math.Max(1, _similarOptions.MaxLimit));

            var cacheKey = CatalogCacheKeys.SimilarProductsPrefix + product.Id.Value;
            var cached = await _cache.GetAsync<SimilarProductsResultDto>(cacheKey, ct);
            if (cached is not null)
                return Result<SimilarProductsResultDto>.Success(cached);

            var detail = await _detailRepository.GetByProductIdAsync(product.Id, ct);
            var tags = detail?.Tags ?? [];
            var brands = detail?.Brands ?? [];

            try
            {
                var esResult = await _similarityService.GetSimilarProductsAsync(
                    product.Id.Value,
                    product.CategoryId.Value,
                    product.Name,
                    product.Description,
                    tags,
                    brands,
                    product.Price.Amount,
                    effectiveLimit,
                    ct);

                if (esResult.IsSuccess)
                {
                    await _cache.SetAsync(cacheKey, esResult.Value!, _ttl.CatalogSimilarProducts, ct);
                    return esResult;
                }

                _logger.LogInformation(
                    "Similar products fallback to DB because Elasticsearch returned failure: {Error}",
                    esResult.Error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Similar products fallback to DB because Elasticsearch query threw");
            }

            var fallback = await BuildDbFallbackAsync(product, detail, effectiveLimit, ct);
            MarketplaceMetrics.CatalogSimilarProductsFallbacks.Add(1);
            await _cache.SetAsync(cacheKey, fallback, _ttl.CatalogSimilarProducts, ct);
            return Result<SimilarProductsResultDto>.Success(fallback);
        }
        catch (Exception ex)
        {
            return Result<SimilarProductsResultDto>.Failure($"Failed to get similar products: {ex.Message}");
        }
    }

    private async Task<SimilarProductsResultDto> BuildDbFallbackAsync(
        Product source,
        ProductDetail? sourceDetail,
        int limit,
        CancellationToken ct)
    {
        var sourceTags = sourceDetail?.Tags ?? [];
        var sourceBrands = sourceDetail?.Brands ?? [];
        var sourcePrice = source.Price.Amount;
        var band = Math.Clamp(_similarOptions.PriceBandPercent, 0, 100) / 100m;
        var minPrice = sourcePrice * (1m - band);
        var maxPrice = sourcePrice * (1m + band);

        var products = await _productRepository.ListActiveAsync(ct);
        var detailsByProductId = new Dictionary<long, ProductDetail?>();
        var rows = new List<(ProductListItemDto Dto, int Score)>();

        foreach (var candidate in products)
        {
            if (candidate.Id == source.Id)
                continue;
            if (candidate.CategoryId != source.CategoryId)
                continue;
            if (candidate.Price.Amount < minPrice || candidate.Price.Amount > maxPrice)
                continue;

            if (!detailsByProductId.TryGetValue(candidate.Id.Value, out var candidateDetail))
            {
                candidateDetail = await _detailRepository.GetByProductIdAsync(candidate.Id, ct);
                detailsByProductId[candidate.Id.Value] = candidateDetail;
            }

            var stockRows = await _stockRepository.ListByProductAsync(candidate.CompanyId, candidate.Id, ct);
            var available = stockRows.Sum(x => x.Available);
            var availability = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
            var dto = ProductMapper.ToListItemDto(candidate, available, availability);

            var score = ScoreCandidate(source.Name, sourceTags, sourceBrands, sourcePrice, candidate, candidateDetail);
            rows.Add((dto, score));
        }

        var items = rows
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Dto.AvailabilityStatus == "in_stock")
            .ThenByDescending(x => x.Dto.CreatedAt)
            .Take(limit)
            .Select(x => x.Dto)
            .ToList();

        return new SimilarProductsResultDto(source.Id.Value, items);
    }

    internal static int ScoreCandidate(
        string sourceName,
        IReadOnlyList<string> sourceTags,
        IReadOnlyList<string> sourceBrands,
        decimal sourcePrice,
        Product candidate,
        ProductDetail? candidateDetail)
    {
        var score = 0;
        var candidateTags = candidateDetail?.Tags ?? [];
        var candidateBrands = candidateDetail?.Brands ?? [];

        foreach (var tag in sourceTags)
        {
            if (candidateTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                score += 3;
        }

        foreach (var brand in sourceBrands)
        {
            if (candidateBrands.Contains(brand, StringComparer.OrdinalIgnoreCase))
                score += 2;
        }

        if (candidate.Name.Contains(sourceName, StringComparison.OrdinalIgnoreCase) ||
            sourceName.Contains(candidate.Name, StringComparison.OrdinalIgnoreCase))
            score += 2;

        if (sourcePrice > 0)
        {
            var delta = Math.Abs(candidate.Price.Amount - sourcePrice) / sourcePrice;
            if (delta <= 0.1m)
                score += 1;
        }

        return score;
    }
}
