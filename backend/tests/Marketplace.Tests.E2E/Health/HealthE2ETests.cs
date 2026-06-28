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
    public async Task Get_Health_Ready_Returns_Ok_With_Checks()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("postgres", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("checks", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_Health_Live_Returns_Ok()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health/live");
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
