using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Infrastructure.External.Search.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Search;

public sealed class ElasticsearchProductSimilarityService : IProductSimilarityService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly SimilarProductsOptions _similarOptions;
    private readonly ProductSearchIndexManager _indexManager;
    private readonly ILogger<ElasticsearchProductSimilarityService> _logger;

    public ElasticsearchProductSimilarityService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        IOptions<SimilarProductsOptions> similarOptions,
        ProductSearchIndexManager indexManager,
        ILogger<ElasticsearchProductSimilarityService> logger)
    {
        _client = client;
        _options = options.Value;
        _similarOptions = similarOptions.Value;
        _indexManager = indexManager;
        _logger = logger;
    }

    public async Task<Result<SimilarProductsResultDto>> GetSimilarProductsAsync(
        long productId,
        long categoryId,
        string name,
        string description,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> brands,
        decimal price,
        int limit,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Result<SimilarProductsResultDto>.Failure("Elasticsearch is disabled");

        try
        {
            await _indexManager.EnsureIndexExistsAsync(ct);

            var size = Math.Clamp(limit, 1, Math.Max(1, _similarOptions.MaxLimit));
            var band = Math.Clamp(_similarOptions.PriceBandPercent, 0, 100) / 100m;
            var minPrice = price * (1m - band);
            var maxPrice = price * (1m + band);

            var response = await _client.SearchAsync<ProductSearchDocument>(s =>
            {
                s.Indices(_indexManager.IndexName);
                s.Size(size);
                s.Query(q => q.Bool(b => b
                    .MustNot(mn => mn.Term(t => t.Field(f => f.Id).Value(productId)))
                    .Filter(
                        f => f.Term(t => t.Field(x => x.Status).Value("active")),
                        f => f.Term(t => t.Field(x => x.CategoryId).Value(categoryId)),
                        f => f.Range(r => r.Number(nr => nr
                            .Field(x => x.Price)
                            .Gte((double)minPrice)
                            .Lte((double)maxPrice))))
                    .Should(
                        sh => sh.MoreLikeThis(mlt => ConfigureMoreLikeThis(mlt, name, description, tags, brands)),
                        sh => sh.Term(t => t.Field(x => x.AvailabilityStatus).Value("in_stock").Boost(2)))
                    .MinimumShouldMatch(1)));
            }, ct);

            if (!response.IsValidResponse)
            {
                return Result<SimilarProductsResultDto>.Failure(
                    $"Elasticsearch similar products failed: {response.ElasticsearchServerError?.Error?.Reason ?? "invalid response"}");
            }

            var items = response.Documents
                .Select(ToProductListItem)
                .ToList();

            return Result<SimilarProductsResultDto>.Success(new SimilarProductsResultDto(productId, items));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch similar products query failed for product {ProductId}", productId);
            return Result<SimilarProductsResultDto>.Failure($"Elasticsearch similar products failed: {ex.Message}");
        }
    }

    private static void ConfigureMoreLikeThis(
        MoreLikeThisQueryDescriptor<ProductSearchDocument> mlt,
        string name,
        string description,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> brands)
    {
        mlt.Fields(f => f.Name, f => f.Description, f => f.Tags, f => f.Brands);
        var likeText = string.Join(' ',
            new[] { name, description }
                .Concat(tags)
                .Concat(brands)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(likeText))
            mlt.Like(l => l.Text(likeText));
        mlt.MinTermFreq(1);
        mlt.MaxQueryTerms(12);
    }

    private static ProductListItemDto ToProductListItem(ProductSearchDocument x) =>
        new(
            x.Id,
            x.CompanyId,
            x.Name,
            x.Slug,
            x.Description,
            x.Price,
            x.OldPrice,
            x.CategoryId,
            x.Status,
            x.HasVariants,
            x.Stock,
            x.MinStock,
            x.AvailableQty,
            x.AvailabilityStatus,
            x.CreatedAt,
            x.UpdatedAt);
}
