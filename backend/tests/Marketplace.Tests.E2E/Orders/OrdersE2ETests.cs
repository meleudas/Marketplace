using System.Net;
using System.Net.Http.Headers;
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
