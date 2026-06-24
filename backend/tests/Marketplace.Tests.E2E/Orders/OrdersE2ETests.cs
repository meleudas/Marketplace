using System.Net;
using System.Net.Http.Headers;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Orders;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Orders")]
[Trait("Layer", "E2E")]
public sealed class OrdersE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public OrdersE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_My_Orders_With_Token_Returns_Success()
    {
        var (client, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/me/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Seed")]
[Trait("Suite", "Orders")]
[Trait("Layer", "E2E")]
public sealed class SeedOrdersE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public SeedOrdersE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Buyer_Can_Get_Seed_Order_Details()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var response = await buyer.GetAsync($"/me/orders/{SeedTestConstants.OrderShippedId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
