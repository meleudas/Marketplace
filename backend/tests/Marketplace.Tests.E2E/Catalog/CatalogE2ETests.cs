using System.Net;
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
