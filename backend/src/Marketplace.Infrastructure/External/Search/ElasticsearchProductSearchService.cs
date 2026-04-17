using Elastic.Clients.Elasticsearch;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Infrastructure.External.Search.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Search;

public sealed class ElasticsearchProductSearchService : IProductSearchService, IProductSearchIndexer
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly IProductRepository _productRepository;
    private readonly IProductDetailRepository _productDetailRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly ILogger<ElasticsearchProductSearchService> _logger;

    public ElasticsearchProductSearchService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        IProductRepository productRepository,
        IProductDetailRepository productDetailRepository,
        IWarehouseStockRepository stockRepository,
        ILogger<ElasticsearchProductSearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _productRepository = productRepository;
        _productDetailRepository = productDetailRepository;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
        string? name,
        IReadOnlyList<long>? categoryIds,
        Guid? companyId,
        decimal? minPrice,
        decimal? maxPrice,
        string? availabilityStatus,
        string? sort,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Result<ProductSearchResultDto>.Failure("Elasticsearch is disabled");

        try
        {
            await EnsureIndexExistsAsync(ct);

            var response = await _client.SearchAsync<ProductSearchDocument>(s =>
            {
                s.Indices(_options.ProductsIndex);
                s.Size(1000);
                if (string.IsNullOrWhiteSpace(name))
                {
                    s.Query(q => q.MatchAll(_ => { }));
                }
                else
                {
                    s.Query(q => q.Match(m => m
                        .Field(f => f.Name)
                        .Query(name)
                        .Operator(Elastic.Clients.Elasticsearch.QueryDsl.Operator.And)));
                }
            }, ct);

            if (!response.IsValidResponse)
                return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {response.ElasticsearchServerError?.Error?.Reason ?? "invalid response"}");

            IEnumerable<ProductSearchDocument> docs = response.Documents;

            if (categoryIds is { Count: > 0 })
                docs = docs.Where(x => categoryIds.Contains(x.CategoryId));
            if (companyId.HasValue)
                docs = docs.Where(x => x.CompanyId == companyId.Value);
            if (minPrice.HasValue)
                docs = docs.Where(x => x.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                docs = docs.Where(x => x.Price <= maxPrice.Value);
            if (!string.IsNullOrWhiteSpace(availabilityStatus))
                docs = docs.Where(x => string.Equals(x.AvailabilityStatus, availabilityStatus.Trim(), StringComparison.OrdinalIgnoreCase));

            docs = (sort ?? "relevance").Trim().ToLowerInvariant() switch
            {
                "price_asc" => docs.OrderBy(x => x.Price),
                "price_desc" => docs.OrderByDescending(x => x.Price),
                "newest" => docs.OrderByDescending(x => x.CreatedAt),
                _ => docs
            };

            var total = docs.LongCount();
            var skip = Math.Max(0, (page - 1) * pageSize);
            var items = docs
                .Skip(skip)
                .Take(pageSize)
                .Select(ToProductListItem)
                .ToList();

            return Result<ProductSearchResultDto>.Success(new ProductSearchResultDto(items, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch search failed");
            return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {ex.Message}");
        }
    }

    public async Task UpsertProductAsync(long productId, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await EnsureIndexExistsAsync(ct);

        var product = await _productRepository.GetByIdAsync(ProductId.From(productId), ct);
        if (product is null || product.IsDeleted || product.Status != ProductStatus.Active)
        {
            await DeleteProductAsync(productId, ct);
            return;
        }

        var detail = await _productDetailRepository.GetByProductIdAsync(product.Id, ct);
        var stockRows = await _stockRepository.ListByProductAsync(product.CompanyId, product.Id, ct);
        var availableQty = stockRows.Sum(x => x.Available);
        var availability = availableQty <= 0 ? "out_of_stock" : availableQty <= 5 ? "low_stock" : "in_stock";

        var document = new ProductSearchDocument
        {
            Id = product.Id.Value,
            CompanyId = product.CompanyId.Value,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price.Amount,
            OldPrice = product.OldPrice?.Amount,
            CategoryId = product.CategoryId.Value,
            Status = product.Status.ToString().ToLowerInvariant(),
            HasVariants = product.HasVariants,
            Stock = product.Stock,
            MinStock = product.MinStock,
            AvailableQty = availableQty,
            AvailabilityStatus = availability,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Tags = detail?.Tags ?? [],
            Brands = detail?.Brands ?? []
        };

        var indexResponse = await _client.IndexAsync(document, i => i
            .Index(_options.ProductsIndex)
            .Id(productId), ct);

        if (!indexResponse.IsValidResponse)
            throw new InvalidOperationException(indexResponse.ElasticsearchServerError?.Error?.Reason ?? "Failed to index product");
    }

    public async Task DeleteProductAsync(long productId, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await EnsureIndexExistsAsync(ct);
        _ = await _client.DeleteAsync(new DeleteRequest(_options.ProductsIndex, productId), ct);
    }

    public async Task FullReindexAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await EnsureIndexExistsAsync(ct);
        var products = await _productRepository.ListActiveAsync(ct);
        foreach (var product in products)
        {
            await UpsertProductAsync(product.Id.Value, ct);
        }
    }

    private async Task EnsureIndexExistsAsync(CancellationToken ct)
    {
        var exists = await _client.Indices.ExistsAsync(_options.ProductsIndex, ct);
        if (exists.Exists)
            return;

        _ = await _client.Indices.CreateAsync(_options.ProductsIndex, cancellationToken: ct);
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
