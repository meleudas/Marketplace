using System.Net;
using System.Net.Http.Headers;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Inventory;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Inventory")]
[Trait("Layer", "E2E")]
public sealed class InventoryE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public InventoryE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task List_Warehouses_Requires_Auth()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/companies/00000000-0000-0000-0000-000000000001/warehouses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_Warehouses_With_Seller_Token_Returns_NotFound_Or_Forbidden()
    {
        var (client, _) = await _fixture.CreateAuthenticatedClientAsync("Seller");

        var response = await client.GetAsync("/companies/00000000-0000-0000-0000-000000000001/warehouses");
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest);
    }
}
