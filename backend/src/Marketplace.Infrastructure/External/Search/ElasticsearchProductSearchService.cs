using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Marketplace.Application.Products.Catalog;
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
using System.Text;
using System.Text.Json;

namespace Marketplace.Infrastructure.External.Search;

public sealed class ElasticsearchProductSearchService : IProductSearchService, IProductSearchIndexer
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ProductSearchIndexManager _indexManager;
    private readonly IProductRepository _productRepository;
    private readonly IProductDetailRepository _productDetailRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly ILogger<ElasticsearchProductSearchService> _logger;

    public ElasticsearchProductSearchService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ProductSearchIndexManager indexManager,
        IProductRepository productRepository,
        IProductDetailRepository productDetailRepository,
        IWarehouseStockRepository stockRepository,
        ILogger<ElasticsearchProductSearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _indexManager = indexManager;
        _productRepository = productRepository;
        _productDetailRepository = productDetailRepository;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<Result<ProductSearchResultDto>> SearchCatalogProductsAsync(
        CatalogProductSearchFilters filters,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Result<ProductSearchResultDto>.Failure("Elasticsearch is disabled");

        try
        {
            await _indexManager.EnsureIndexExistsAsync(ct);

            var pageSize = Math.Clamp(filters.PageSize, 1, 200);
            var page = filters.Page <= 0 ? 1 : filters.Page;
            var cursorValues = DecodeSearchAfter(filters.SearchAfter);
            var response = await _client.SearchAsync<ProductSearchDocument>(s =>
            {
                s.Indices(_indexManager.IndexName);
                s.Size(pageSize);
                ApplySort(s, filters.Sort);
                if (cursorValues.Count > 0)
                    s.SearchAfter(cursorValues);
                else if (page > 1)
                    s.From(Math.Max(0, (page - 1) * pageSize));

                s.Query(q => q.Bool(b => ConfigureSearchBoolQuery(b, filters)));
            }, ct);

            if (!response.IsValidResponse)
                return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {response.ElasticsearchServerError?.Error?.Reason ?? "invalid response"}");

            var docs = response.Documents?.ToList() ?? [];
            var items = docs.Select(ToProductListItem).ToList();
            var total = response.Total;
            var nextSearchAfter = docs.Count > 0
                ? EncodeSearchAfter(BuildSearchAfterValues(docs[^1], filters.Sort))
                : null;

            return Result<ProductSearchResultDto>.Success(new ProductSearchResultDto(items, total, page, pageSize, nextSearchAfter));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch search failed");
            return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {ex.Message}");
        }
    }

    public async Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleProductsAsync(
        CatalogOnSaleProductFilters filters,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Result<ProductSearchResultDto>.Failure("Elasticsearch is disabled");

        try
        {
            await _indexManager.EnsureIndexExistsAsync(ct);

            var pageSize = Math.Clamp(filters.PageSize, 1, 200);
            var page = filters.Page <= 0 ? 1 : filters.Page;
            var cursorValues = DecodeSearchAfter(filters.SearchAfter);
            var response = await _client.SearchAsync<ProductSearchDocument>(s =>
            {
                s.Indices(_indexManager.IndexName);
                s.Size(pageSize);
                ApplyOnSaleSort(s, filters.Sort);
                if (cursorValues.Count > 0)
                    s.SearchAfter(cursorValues);
                else if (page > 1)
                    s.From(Math.Max(0, (page - 1) * pageSize));

                s.Query(q => q.Bool(b => ConfigureOnSaleBoolQuery(b, filters)));
            }, ct);

            if (!response.IsValidResponse)
                return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {response.ElasticsearchServerError?.Error?.Reason ?? "invalid response"}");

            var docs = response.Documents?.ToList() ?? [];
            var items = docs.Select(ToProductListItem).ToList();
            var total = response.Total;
            var nextSearchAfter = docs.Count > 0
                ? EncodeSearchAfter(BuildOnSaleSearchAfterValues(docs[^1], filters.Sort))
                : null;

            return Result<ProductSearchResultDto>.Success(new ProductSearchResultDto(items, total, page, pageSize, nextSearchAfter));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch on-sale search failed");
            return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {ex.Message}");
        }
    }

    public Task<Result<ProductSearchResultDto>> SearchCatalogNewProductsAsync(
        CatalogBrowsableProductFilters filters,
        CancellationToken ct = default)
        => SearchBrowsableProductsAsync(filters, BrowseSortMode.Newest, ct);

    public Task<Result<ProductSearchResultDto>> SearchCatalogPopularProductsAsync(
        CatalogBrowsableProductFilters filters,
        CancellationToken ct = default)
        => SearchBrowsableProductsAsync(filters, BrowseSortMode.Popular, ct);

    private async Task<Result<ProductSearchResultDto>> SearchBrowsableProductsAsync(
        CatalogBrowsableProductFilters filters,
        BrowseSortMode sortMode,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ProductSearchResultDto>.Failure("Elasticsearch is disabled");

        try
        {
            await _indexManager.EnsureIndexExistsAsync(ct);

            var pageSize = Math.Clamp(filters.PageSize, 1, 200);
            var page = filters.Page <= 0 ? 1 : filters.Page;
            var cursorValues = DecodeSearchAfter(filters.SearchAfter);
            var response = await _client.SearchAsync<ProductSearchDocument>(s =>
            {
                s.Indices(_indexManager.IndexName);
                s.Size(pageSize);
                ApplyBrowsableSort(s, sortMode);
                if (cursorValues.Count > 0)
                    s.SearchAfter(cursorValues);
                else if (page > 1)
                    s.From(Math.Max(0, (page - 1) * pageSize));

                s.Query(q => q.Bool(b => ConfigureBrowsableBoolQuery(b, filters)));
            }, ct);

            if (!response.IsValidResponse)
                return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {response.ElasticsearchServerError?.Error?.Reason ?? "invalid response"}");

            var docs = response.Documents?.ToList() ?? [];
            var items = docs.Select(ToProductListItem).ToList();
            var total = response.Total;
            var nextSearchAfter = docs.Count > 0
                ? EncodeSearchAfter(BuildBrowsableSearchAfterValues(docs[^1], sortMode))
                : null;

            return Result<ProductSearchResultDto>.Success(new ProductSearchResultDto(items, total, page, pageSize, nextSearchAfter));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch browsable search failed");
            return Result<ProductSearchResultDto>.Failure($"Elasticsearch search failed: {ex.Message}");
        }
    }

    public async Task UpsertProductAsync(long productId, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await _indexManager.EnsureIndexExistsAsync(ct);

        var product = await _productRepository.GetByIdAsync(ProductId.From(productId), ct);
        if (product is null || product.IsDeleted || product.Status != ProductStatus.Active)
        {
            await DeleteProductAsync(productId, ct);
            return;
        }

        var detail = await _productDetailRepository.GetByProductIdAsync(product.Id, ct);
        var facets = ProductCatalogFacetReader.Read(
            detail?.Attributes ?? JsonBlob.Empty,
            detail?.Tags ?? [],
            detail?.Brands ?? []);
        var stockRows = await _stockRepository.ListByProductAsync(product.CompanyId, product.Id, ct);
        var availableQty = stockRows.Sum(x => x.Available);
        var availability = availableQty <= 0 ? "out_of_stock" : availableQty <= 5 ? "low_stock" : "in_stock";

        var price = product.Price.Amount;
        var oldPrice = product.OldPrice?.Amount;
        var discountPercent = ProductDiscount.Percent(price, oldPrice);

        var document = new ProductSearchDocument
        {
            Id = product.Id.Value,
            CompanyId = product.CompanyId.Value,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = price,
            OldPrice = oldPrice,
            DiscountPercent = discountPercent,
            IsOnSale = discountPercent.HasValue,
            CategoryId = product.CategoryId.Value,
            Status = product.Status.ToString().ToLowerInvariant(),
            HasVariants = product.HasVariants,
            Stock = product.Stock,
            MinStock = product.MinStock,
            AvailableQty = availableQty,
            AvailabilityStatus = availability,
            ViewCount = product.ViewCount,
            SalesCount = product.SalesCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Tags = facets.Tags,
            Brands = detail?.Brands ?? [],
            Author = facets.Author,
            Format = facets.Format,
            Genres = facets.Genres
        };

        var indexResponse = await _client.IndexAsync(document, i => i
            .Index(_indexManager.IndexName)
            .Id(productId), ct);

        if (!indexResponse.IsValidResponse)
            throw new InvalidOperationException(indexResponse.ElasticsearchServerError?.Error?.Reason ?? "Failed to index product");
    }

    public async Task DeleteProductAsync(long productId, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await _indexManager.EnsureIndexExistsAsync(ct);
        _ = await _client.DeleteAsync(new DeleteRequest(_indexManager.IndexName, productId), ct);
    }

    public async Task FullReindexAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await _indexManager.EnsureIndexExistsAsync(ct);
        var products = await _productRepository.ListActiveAsync(ct);
        foreach (var product in products)
        {
            await UpsertProductAsync(product.Id.Value, ct);
        }
    }

    private static void ApplySort(SearchRequestDescriptor<ProductSearchDocument> s, string? sort)
    {
        switch ((sort ?? "relevance").Trim().ToLowerInvariant())
        {
            case "price_asc":
                s.Sort(so => so
                    .Field(f => f.Price, fs => fs.Order(SortOrder.Asc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
            case "price_desc":
                s.Sort(so => so
                    .Field(f => f.Price, fs => fs.Order(SortOrder.Desc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
            case "newest":
                s.Sort(so => so
                    .Field(f => f.CreatedAt, fs => fs.Order(SortOrder.Desc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
            default:
                s.Sort(so => so
                    .Field(f => f.CreatedAt, fs => fs.Order(SortOrder.Desc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
        }
    }

    private static void ConfigureSearchBoolQuery(BoolQueryDescriptor<ProductSearchDocument> b, CatalogProductSearchFilters filters)
    {
        b.Filter(f => f.Term(t => t.Field(x => x.Status).Value("active")));

        if (!string.IsNullOrWhiteSpace(filters.Name))
        {
            var searchName = filters.Name.Trim();
            if (searchName.Length <= 2)
            {
                b.Must(m => m.MatchPhrasePrefix(prefix => prefix
                    .Field(x => x.Name)
                    .Query(searchName)));
            }
            else
            {
                b.Must(m => m.Bool(bb => bb
                    .Should(
                        s => s.Match(match => match
                            .Field(x => x.Name)
                            .Query(searchName)
                            .Fuzziness(new Fuzziness("AUTO"))
                            .PrefixLength(1)
                            .MaxExpansions(50)
                            .Operator(Operator.Or)),
                        s => s.MatchPhrasePrefix(prefix => prefix
                            .Field(x => x.Name)
                            .Query(searchName)))
                    .MinimumShouldMatch(1)));
            }
        }

        if (filters.CategoryIds is { Count: > 0 })
        {
            b.Filter(f => f.Terms(t => t
                .Field(field => field.CategoryId)
                .Terms(new TermsQueryField(filters.CategoryIds.Select(FieldValue.Long).ToArray()))));
        }

        if (filters.CompanyId.HasValue)
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.CompanyId)
                .Value(filters.CompanyId.Value.ToString())));
        }

        if (filters.MinPrice.HasValue || filters.MaxPrice.HasValue)
        {
            b.Filter(f => f.Range(r => r.Number(nr =>
            {
                nr.Field(field => field.Price);
                if (filters.MinPrice.HasValue)
                    nr.Gte((double)filters.MinPrice.Value);
                if (filters.MaxPrice.HasValue)
                    nr.Lte((double)filters.MaxPrice.Value);
            })));
        }

        if (!string.IsNullOrWhiteSpace(filters.AvailabilityStatus))
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.AvailabilityStatus)
                .Value(filters.AvailabilityStatus.Trim().ToLowerInvariant())));
        }

        if (filters.Authors is { Count: > 0 })
        {
            var authorValues = ProductCatalogFacetReader.NormalizeAuthors(filters.Authors)
                .Select(FieldValue.String)
                .ToArray();
            if (authorValues.Length > 0)
            {
                b.Filter(f => f.Bool(bb => bb
                    .Should(
                        s => s.Terms(t => t
                            .Field(field => field.Author)
                            .Terms(new TermsQueryField(authorValues))),
                        s => s.Terms(t => t
                            .Field(field => field.Brands)
                            .Terms(new TermsQueryField(authorValues))))
                    .MinimumShouldMatch(1)));
            }
        }

        if (!string.IsNullOrWhiteSpace(filters.Format))
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.Format)
                .Value(ProductCatalogFacetReader.Normalize(filters.Format))));
        }

        if (filters.Genres is { Count: > 0 })
        {
            var genreValues = ProductCatalogFacetReader.NormalizeGenres(filters.Genres)
                .Select(FieldValue.String)
                .ToArray();
            if (genreValues.Length > 0)
            {
                b.Filter(f => f.Bool(bb => bb
                    .Should(
                        s => s.Terms(t => t
                            .Field(field => field.Genres)
                            .Terms(new TermsQueryField(genreValues))),
                        s => s.Terms(t => t
                            .Field(field => field.Tags)
                            .Terms(new TermsQueryField(genreValues))))
                    .MinimumShouldMatch(1)));
            }
        }

        if (filters.Tags is { Count: > 0 })
        {
            foreach (var tag in filters.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                b.Filter(f => f.Term(t => t
                    .Field(field => field.Tags)
                    .Value(ProductCatalogFacetReader.Normalize(tag))));
            }
        }

        if (string.IsNullOrWhiteSpace(filters.Name))
            b.Must(m => m.MatchAll(_ => { }));
    }

    private static void ApplyOnSaleSort(SearchRequestDescriptor<ProductSearchDocument> s, string? sort)
    {
        switch ((sort ?? "discount_desc").Trim().ToLowerInvariant())
        {
            case "price_asc":
                s.Sort(so => so
                    .Field(f => f.Price, fs => fs.Order(SortOrder.Asc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
            case "price_desc":
                s.Sort(so => so
                    .Field(f => f.Price, fs => fs.Order(SortOrder.Desc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
            case "newest":
                s.Sort(so => so
                    .Field(f => f.CreatedAt, fs => fs.Order(SortOrder.Desc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
            default:
                s.Sort(so => so
                    .Field(f => f.DiscountPercent, fs => fs.Order(SortOrder.Desc))
                    .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
                break;
        }
    }

    private static void ConfigureOnSaleBoolQuery(BoolQueryDescriptor<ProductSearchDocument> b, CatalogOnSaleProductFilters filters)
    {
        b.Filter(f => f.Term(t => t.Field(x => x.Status).Value("active")));
        b.Filter(f => f.Term(t => t.Field(x => x.IsOnSale).Value(true)));

        if (filters.CategoryIds is { Count: > 0 })
        {
            b.Filter(f => f.Terms(t => t
                .Field(field => field.CategoryId)
                .Terms(new TermsQueryField(filters.CategoryIds.Select(FieldValue.Long).ToArray()))));
        }

        if (filters.CompanyId.HasValue)
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.CompanyId)
                .Value(filters.CompanyId.Value.ToString())));
        }

        if (filters.MinPrice.HasValue || filters.MaxPrice.HasValue)
        {
            b.Filter(f => f.Range(r => r.Number(nr =>
            {
                nr.Field(field => field.Price);
                if (filters.MinPrice.HasValue)
                    nr.Gte((double)filters.MinPrice.Value);
                if (filters.MaxPrice.HasValue)
                    nr.Lte((double)filters.MaxPrice.Value);
            })));
        }

        if (filters.MinDiscountPercent.HasValue)
        {
            b.Filter(f => f.Range(r => r.Number(nr => nr
                .Field(field => field.DiscountPercent)
                .Gte((double)filters.MinDiscountPercent.Value))));
        }

        if (!string.IsNullOrWhiteSpace(filters.AvailabilityStatus))
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.AvailabilityStatus)
                .Value(filters.AvailabilityStatus.Trim().ToLowerInvariant())));
        }

        b.Must(m => m.MatchAll(_ => { }));
    }

    private static IReadOnlyCollection<FieldValue> BuildOnSaleSearchAfterValues(ProductSearchDocument doc, string? sort)
    {
        return (sort ?? "discount_desc").Trim().ToLowerInvariant() switch
        {
            "price_asc" or "price_desc" => [FieldValue.Double((double)doc.Price), FieldValue.Long(doc.Id)],
            "newest" => [FieldValue.String(doc.CreatedAt.ToString("O")), FieldValue.Long(doc.Id)],
            _ => [FieldValue.Double((double)(doc.DiscountPercent ?? 0m)), FieldValue.Long(doc.Id)]
        };
    }

    private static IReadOnlyCollection<FieldValue> BuildSearchAfterValues(ProductSearchDocument doc, string? sort)
    {
        return (sort ?? "relevance").Trim().ToLowerInvariant() switch
        {
            "price_asc" or "price_desc" => [FieldValue.Double((double)doc.Price), FieldValue.Long(doc.Id)],
            _ => [FieldValue.String(doc.CreatedAt.ToString("O")), FieldValue.Long(doc.Id)]
        };
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
            x.DiscountPercent,
            x.CategoryId,
            x.Status,
            x.HasVariants,
            x.Stock,
            x.MinStock,
            x.AvailableQty,
            x.AvailabilityStatus,
            x.CreatedAt,
            x.UpdatedAt,
            []);

    private static string? EncodeSearchAfter(IReadOnlyCollection<FieldValue>? values)
    {
        if (values is null || values.Count == 0)
            return null;
        var raw = values.Select(v => v.ToString()).ToArray();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(raw)));
    }

    private static List<FieldValue> DecodeSearchAfter(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return [];

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var arr = JsonSerializer.Deserialize<string[]>(json) ?? [];
            return arr.Select(FieldValue.String).ToList();
        }
        catch
        {
            return [];
        }
    }

    private enum BrowseSortMode
    {
        Newest,
        Popular
    }

    private static void ApplyBrowsableSort(SearchRequestDescriptor<ProductSearchDocument> s, BrowseSortMode sortMode)
    {
        if (sortMode == BrowseSortMode.Popular)
        {
            s.Sort(so => so
                .Field(f => f.SalesCount, fs => fs.Order(SortOrder.Desc))
                .Field(f => f.ViewCount, fs => fs.Order(SortOrder.Desc))
                .Field(f => f.CreatedAt, fs => fs.Order(SortOrder.Desc))
                .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
            return;
        }

        s.Sort(so => so
            .Field(f => f.CreatedAt, fs => fs.Order(SortOrder.Desc))
            .Field(f => f.Id, fs => fs.Order(SortOrder.Asc)));
    }

    private static void ConfigureBrowsableBoolQuery(BoolQueryDescriptor<ProductSearchDocument> b, CatalogBrowsableProductFilters filters)
    {
        b.Filter(f => f.Term(t => t.Field(x => x.Status).Value("active")));

        if (filters.CategoryIds is { Count: > 0 })
        {
            b.Filter(f => f.Terms(t => t
                .Field(field => field.CategoryId)
                .Terms(new TermsQueryField(filters.CategoryIds.Select(FieldValue.Long).ToArray()))));
        }

        if (filters.CompanyId.HasValue)
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.CompanyId)
                .Value(filters.CompanyId.Value.ToString())));
        }

        if (filters.MinPrice.HasValue || filters.MaxPrice.HasValue)
        {
            b.Filter(f => f.Range(r => r.Number(nr =>
            {
                nr.Field(field => field.Price);
                if (filters.MinPrice.HasValue)
                    nr.Gte((double)filters.MinPrice.Value);
                if (filters.MaxPrice.HasValue)
                    nr.Lte((double)filters.MaxPrice.Value);
            })));
        }

        if (!string.IsNullOrWhiteSpace(filters.AvailabilityStatus))
        {
            b.Filter(f => f.Term(t => t
                .Field(field => field.AvailabilityStatus)
                .Value(filters.AvailabilityStatus.Trim().ToLowerInvariant())));
        }

        b.Must(m => m.MatchAll(_ => { }));
    }

    private static IReadOnlyCollection<FieldValue> BuildBrowsableSearchAfterValues(ProductSearchDocument doc, BrowseSortMode sortMode)
    {
        if (sortMode == BrowseSortMode.Popular)
        {
            return
            [
                FieldValue.Long(doc.SalesCount),
                FieldValue.Long(doc.ViewCount),
                FieldValue.String(doc.CreatedAt.ToString("O")),
                FieldValue.Long(doc.Id)
            ];
        }

        return [FieldValue.String(doc.CreatedAt.ToString("O")), FieldValue.Long(doc.Id)];
    }
}
