using System.Net;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.IdentityAccess;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "IdentityAccess")]
[Trait("Layer", "E2E")]
public sealed class AuthAccountE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public AuthAccountE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Register_And_Login_Flow_Works()
    {
        var client = _fixture.Factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var userName = $"user_{Guid.NewGuid():N}"[..16];
        const string password = "StrongPass1!Aa";

        var register = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, password, userName, null));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password, false, null));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
