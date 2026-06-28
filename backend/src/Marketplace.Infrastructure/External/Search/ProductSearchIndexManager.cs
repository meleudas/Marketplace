using Elastic.Clients.Elasticsearch;
using Marketplace.Infrastructure.External.Search.Documents;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Search;

public sealed class ProductSearchIndexManager
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;

    public ProductSearchIndexManager(ElasticsearchClient client, IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public string IndexName => _options.ProductsIndex;

    public async Task EnsureIndexExistsAsync(CancellationToken ct = default)
    {
        var exists = await _client.Indices.ExistsAsync(_options.ProductsIndex, ct);
        if (exists.Exists)
            return;

        var response = await _client.Indices.CreateAsync(_options.ProductsIndex, c => c
            .Mappings(m => m
                .Properties<ProductSearchDocument>(props => props
                    .LongNumber(n => n.Id)
                    .Keyword(k => k.CompanyId)
                    .Text(t => t.Name)
                    .Keyword(k => k.Slug)
                    .Text(t => t.Description)
                    .DoubleNumber(n => n.Price)
                    .DoubleNumber(n => n.OldPrice)
                    .LongNumber(n => n.CategoryId)
                    .Keyword(k => k.Status)
                    .Boolean(b => b.HasVariants)
                    .IntegerNumber(n => n.Stock)
                    .IntegerNumber(n => n.MinStock)
                    .IntegerNumber(n => n.AvailableQty)
                    .Keyword(k => k.AvailabilityStatus)
                    .Date(d => d.CreatedAt)
                    .Date(d => d.UpdatedAt)
                    .Keyword(k => k.Tags)
                    .Keyword(k => k.Brands))), ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(response.ElasticsearchServerError?.Error?.Reason ?? "Failed to create products index");
    }
}
