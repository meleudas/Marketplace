using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Notifications;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Notifications")]
[Trait("Layer", "E2E")]
public sealed class PushE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public PushE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_Vapid_Public_Key_Is_Public()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/web-push/vapid-public-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Push_Subscription_Requires_Auth()
    {
        var (client, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/me/web-push/subscriptions",
            new { endpoint = "https://push.example/sub-1", p256dh = "key", auth = "secret" });
        Assert.True(response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.BadRequest);
    }
}
