using Marketplace.Application.Common.Observability;
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
using Microsoft.Extensions.Options;
using Microsoft.ML.Data;

namespace Marketplace.Infrastructure.External.Recommendations;

public sealed class MlNetPersonalizedRecommendationService : IPersonalizedRecommendationService
{
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly RecommendationModelLoader _loader;
    private readonly RecommendationModelOptions _options;

    public MlNetPersonalizedRecommendationService(
        IProductRepository productRepository,
        IWarehouseStockRepository stockRepository,
        RecommendationModelLoader loader,
        IOptions<RecommendationModelOptions> options)
    {
        _productRepository = productRepository;
        _stockRepository = stockRepository;
        _loader = loader;
        _options = options.Value;
    }

    public async Task<Result<PersonalizedRecommendationsResultDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? _options.TopK : limit, 1, Math.Max(1, _options.TopK));
        var all = await _productRepository.ListActiveAsync(ct);
        var candidates = all
            .Where(x => !x.IsDeleted && x.Status == ProductStatus.Active)
            .Take(Math.Max(effectiveLimit, _options.CandidatePoolSize))
            .ToList();

        var runtime = await _loader.GetActiveAsync(ct);
        if (runtime is null || !_options.Enabled)
        {
            var fallbackItems = await MapTopByFreshnessAsync(candidates.Take(effectiveLimit), ct);
            MarketplaceMetrics.RecommendationFallbacks.Add(1, [new KeyValuePair<string, object?>("reason", "ml_model_unavailable")]);
            return Result<PersonalizedRecommendationsResultDto>.Success(
                new PersonalizedRecommendationsResultDto(userId, "fallback", true, fallbackItems));
        }

        var scored = new List<(Product Product, float Score)>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var input = new MlRecommendationInput
            {
                UserId = userId.ToString("N"),
                ProductId = candidate.Id.Value.ToString()
            };

            var source = runtime.MlContext.Data.LoadFromEnumerable([input]);
            var transformed = runtime.Model.Transform(source);
            var prediction = runtime.MlContext.Data.CreateEnumerable<MlRecommendationPrediction>(transformed, reuseRowObject: false).FirstOrDefault();
            scored.Add((candidate, prediction?.Score ?? 0f));
        }

        var topProducts = scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.CreatedAt)
            .Take(effectiveLimit)
            .Select(x => x.Product)
            .ToList();

        var items = await MapTopByFreshnessAsync(topProducts, ct);
        return Result<PersonalizedRecommendationsResultDto>.Success(
            new PersonalizedRecommendationsResultDto(userId, runtime.Version, false, items));
    }

    private async Task<IReadOnlyList<ProductListItemDto>> MapTopByFreshnessAsync(IEnumerable<Product> products, CancellationToken ct)
    {
        var result = new List<ProductListItemDto>();
        foreach (var product in products)
        {
            var stockRows = await _stockRepository.ListByProductAsync(product.CompanyId, ProductId.From(product.Id.Value), ct);
            var available = stockRows.Sum(x => x.Available);
            var availability = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
            result.Add(ProductMapper.ToListItemDto(product, available, availability));
        }

        return result;
    }

    private sealed class MlRecommendationInput
    {
        public string UserId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
    }

    private sealed class MlRecommendationPrediction
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
