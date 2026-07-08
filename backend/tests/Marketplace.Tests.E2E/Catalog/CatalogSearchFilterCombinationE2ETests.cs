using System.Net;
using System.Net.Http.Json;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Tests.Common.Catalog;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Catalog;

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "CatalogSearchFilters")]
[Trait("Layer", "E2E")]
public sealed class CatalogSearchFilterCombinationE2ETests : IAsyncLifetime
{
    private static int _searchIndexInitialized;
    private readonly MarketplaceSeededE2EFixture _fixture;

    public CatalogSearchFilterCombinationE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    public static IEnumerable<object[]> SeedE2ECombinations()
        => CatalogSearchFilterCombinationGenerator.SeedE2ECombinations();

    public async Task InitializeAsync()
    {
        if (Interlocked.Exchange(ref _searchIndexInitialized, 1) == 1)
            return;

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IProductSearchIndexer>();
        await indexer.FullReindexAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [MemberData(nameof(SeedE2ECombinations))]
    public async Task Http_Search_Combines_Filter_Dimensions_With_And_Semantics_For_Seed_Anchors(
        byte mask,
        SearchCatalogProductsQuery query,
        string[] expectedSlugs)
    {
        var client = _fixture.Factory.CreateClient();
        var queryString = CatalogSearchFilterQueryStringBuilder.Build(query);
        var response = await client.GetAsync($"/catalog/products/search?{queryString}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ProductSearchResultDto>(E2EJsonOptions.Default);
        Assert.NotNull(payload);

        var actualSlugs = payload!.Items.Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        var anchorSlugs = CatalogSearchFilterOracle.SeedAnchorProducts
            .Select(product => product.Slug)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expectedSlug in expectedSlugs)
            Assert.Contains(expectedSlug, actualSlugs);

        foreach (var actualAnchorSlug in actualSlugs.Where(anchorSlugs.Contains))
        {
            var product = CatalogSearchFilterOracle.SeedAnchorProducts
                .First(fixtureProduct => fixtureProduct.Slug == actualAnchorSlug);
            Assert.True(
                CatalogSearchFilterOracle.Matches(
                    product,
                    CatalogSearchFilterOracle.SeedFilterValues,
                    mask),
                $"Anchor product '{actualAnchorSlug}' matched filters but should not for mask {mask}.");
        }
    }
}
