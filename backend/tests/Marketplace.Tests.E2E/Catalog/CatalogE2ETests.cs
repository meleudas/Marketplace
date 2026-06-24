using System.Net;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Catalog;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "CatalogCategories")]
[Trait("Layer", "E2E")]
public sealed class CatalogE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public CatalogE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Search_Catalog_Products_Returns_Success()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/catalog/products?q=test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Seed")]
[Trait("Suite", "CatalogCategories")]
[Trait("Layer", "E2E")]
public sealed class SeedCatalogE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public SeedCatalogE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_Seed_Product_By_Slug_Returns_Success()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync($"/catalog/products/{SeedTestConstants.ProductSlug}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
