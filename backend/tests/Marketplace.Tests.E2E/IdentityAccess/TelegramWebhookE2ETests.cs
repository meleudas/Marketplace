using System.Net;
using System.Net.Http.Json;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.IdentityAccess;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "IdentityAccess")]
[Trait("Layer", "E2E")]
public sealed class TelegramWebhookE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public TelegramWebhookE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Webhook_With_Invalid_Secret_Returns_Unauthorized()
    {
        var client = _fixture.Factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/integrations/telegram/webhook")
        {
            Content = JsonContent.Create(new TelegramUpdateDto(
                new TelegramMessageDto(new TelegramChatDto(1), "/start code")))
        };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", "wrong-secret");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_With_Valid_Secret_Accepts_Request()
    {
        var client = _fixture.Factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/integrations/telegram/webhook")
        {
            Content = JsonContent.Create(new TelegramUpdateDto(
                new TelegramMessageDto(new TelegramChatDto(1), "hello")))
        };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", "e2e-telegram-secret");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
