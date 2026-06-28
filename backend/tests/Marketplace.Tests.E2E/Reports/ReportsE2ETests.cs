using System.Net;
using System.Net.Http.Json;
using Marketplace.Tests.Fixtures;

namespace Marketplace.Tests.Reports;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Reports")]
[Trait("Layer", "E2E")]
public sealed class ReportsE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public ReportsE2ETests(MarketplaceE2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Report_Without_Auth_Returns_Unauthorized()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/reports", new
        {
            targetType = 0,
            targetId = "product-1",
            reason = 0,
            description = "abuse",
            priority = 2,
            images = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
