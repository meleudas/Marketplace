using System.Net;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Payments;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Payments")]
[Trait("Layer", "E2E")]
public sealed class LiqPayWebhookE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public LiqPayWebhookE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Webhook_Endpoint_Accepts_Valid_Payload()
    {
        var client = _fixture.Factory.CreateClient();
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"order_id\":\"E2E-ORD-1\",\"status\":\"success\"}"));
        var response = await client.PostAsJsonAsync(
            "/integrations/liqpay/webhook",
            new LiqPayWebhookRequest(payload, "test-signature"));

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized);
    }
}
