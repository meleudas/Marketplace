using System.Net;
using System.Net.Http.Json;
using Marketplace.Tests.Fixtures;

namespace Marketplace.Tests.Support;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Support")]
[Trait("Layer", "E2E")]
public sealed class SupportE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public SupportE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_Ticket_Without_Auth_Returns_Unauthorized()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/support/tickets",
            new { subject = "Help", message = "Issue", priority = (short)1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
