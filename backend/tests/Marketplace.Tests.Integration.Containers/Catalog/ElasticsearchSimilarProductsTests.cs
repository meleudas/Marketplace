using Elastic.Clients.Elasticsearch;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.External.Search;
using Marketplace.Infrastructure.External.Search.Documents;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Catalog;

[Trait("Suite", "IntegrationContainers")]
[Collection(nameof(MarketplaceContainersCollection))]
public sealed class ElasticsearchSimilarProductsTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ElasticsearchSimilarProductsTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SimilarProducts_Excludes_Source_And_Returns_Same_Category()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ElasticsearchClient>();
        var indexManager = scope.ServiceProvider.GetRequiredService<ProductSearchIndexManager>();
        var similarity = scope.ServiceProvider.GetRequiredService<IProductSimilarityService>();

        await indexManager.EnsureIndexExistsAsync();
        var indexName = indexManager.IndexName;

        var now = DateTime.UtcNow;
        var documents = new[]
        {
            new ProductSearchDocument
            {
                Id = 101,
                CompanyId = Guid.NewGuid(),
                Name = "Gaming Keyboard Pro",
                Slug = "gaming-keyboard-pro",
                Description = "Mechanical RGB keyboard",
                Price = 120,
                CategoryId = 5,
                Status = "active",
                AvailabilityStatus = "in_stock",
                AvailableQty = 10,
                Tags = ["gaming", "keyboard"],
                Brands = ["acme"],
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProductSearchDocument
            {
                Id = 102,
                CompanyId = Guid.NewGuid(),
                Name = "Gaming Keyboard Lite",
                Slug = "gaming-keyboard-lite",
                Description = "Compact gaming keyboard",
                Price = 95,
                CategoryId = 5,
                Status = "active",
                AvailabilityStatus = "in_stock",
                AvailableQty = 8,
                Tags = ["gaming", "keyboard"],
                Brands = ["acme"],
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProductSearchDocument
            {
                Id = 103,
                CompanyId = Guid.NewGuid(),
                Name = "Office Mouse",
                Slug = "office-mouse",
                Description = "Basic office mouse",
                Price = 25,
                CategoryId = 6,
                Status = "active",
                AvailabilityStatus = "in_stock",
                AvailableQty = 20,
                Tags = ["office"],
                Brands = ["acme"],
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var doc in documents)
        {
            var indexResponse = await client.IndexAsync(doc, i => i.Index(indexName).Id(doc.Id));
            Assert.True(indexResponse.IsValidResponse, indexResponse.ElasticsearchServerError?.Error?.Reason);
        }

        await client.Indices.RefreshAsync(indexName);

        var result = await similarity.GetSimilarProductsAsync(
            101,
            5,
            documents[0].Name,
            documents[0].Description,
            documents[0].Tags,
            documents[0].Brands,
            documents[0].Price,
            10,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!.Items, x => x.Id == 101);
        Assert.All(result.Value.Items, x => Assert.Equal(5, x.CategoryId));
        Assert.Contains(result.Value.Items, x => x.Id == 102);
    }
}
