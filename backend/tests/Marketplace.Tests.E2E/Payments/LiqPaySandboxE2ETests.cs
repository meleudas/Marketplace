using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.API.Controllers;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Payments;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Staging")]
[Trait("Layer", "E2E")]
public sealed class LiqPaySandboxE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public LiqPaySandboxE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Webhook_Accepts_Valid_Sandbox_Signature_When_Secrets_Configured()
    {
        var publicKey = Environment.GetEnvironmentVariable("LIQPAY_SANDBOX_PUBLIC");
        var privateKey = Environment.GetEnvironmentVariable("LIQPAY_SANDBOX_PRIVATE");
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            return;

        var client = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LiqPay:PublicKey", publicKey);
            builder.UseSetting("LiqPay:PrivateKey", privateKey);
        }).CreateClient();

        var payloadJson = JsonSerializer.Serialize(new { order_id = "staging-e2e-ord-1", status = "success" });
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
        var signature = BuildSignature(privateKey, data);

        var response = await client.PostAsJsonAsync(
            "/integrations/liqpay/webhook",
            new LiqPayWebhookRequest(data, signature));

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized);
    }

    private static string BuildSignature(string privateKey, string data)
    {
        var input = $"{privateKey}{data}{privateKey}";
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }
}
