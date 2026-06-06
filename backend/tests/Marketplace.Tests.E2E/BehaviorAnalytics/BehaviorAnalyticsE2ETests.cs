using System.Net;
using Marketplace.Tests.Fixtures;

namespace Marketplace.Tests.BehaviorAnalytics;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "BehaviorAnalytics")]
[Trait("Layer", "E2E")]
public sealed class BehaviorAnalyticsE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public BehaviorAnalyticsE2ETests(MarketplaceE2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminSummary_WithoutAuth_Returns_Unauthorized()
    {
        var client = _fixture.Factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.GetAsync($"/admin/analytics/kpi/summary?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
