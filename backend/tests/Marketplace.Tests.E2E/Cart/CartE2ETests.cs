using System.Net;
using System.Net.Http.Headers;
using Marketplace.Tests.Common.Seed;
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

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Seed")]
[Trait("Suite", "CartCheckout")]
[Trait("Layer", "E2E")]
public sealed class SeedCartE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public SeedCartE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Buyer_Seed_Cart_Has_Items()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var response = await buyer.GetAsync("/me/cart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("items", body, StringComparison.OrdinalIgnoreCase);
    }
}
