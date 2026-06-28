using System.Net;
using System.Net.Http.Json;
using Marketplace.Tests.Fixtures;

namespace Marketplace.Tests.Chats;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Chats")]
[Trait("Layer", "E2E")]
public sealed class ChatsE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public ChatsE2ETests(MarketplaceE2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Chat_Without_Auth_Returns_Unauthorized()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/me/chats", new { type = (short)1, productId = (long?)null, orderId = (long?)null });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
