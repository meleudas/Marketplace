using System.Net;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Health;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Platform")]
[Trait("Layer", "E2E")]
public sealed class HealthE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public HealthE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_Health_Returns_Ok()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_LiqPay_Config_Health_Returns_Ok()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health/liqpay/config");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
