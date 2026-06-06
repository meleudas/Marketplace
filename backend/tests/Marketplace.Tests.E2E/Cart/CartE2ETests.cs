using System.Net;
using System.Net.Http.Headers;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Cart;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "CartCheckout")]
[Trait("Layer", "E2E")]
public sealed class CartE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public CartE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_My_Cart_Without_Token_Returns_Unauthorized()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/me/cart");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_My_Cart_With_Token_Returns_Success()
    {
        var (client, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/me/cart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
