using System.Security.Claims;
using Marketplace.Application.Auth.Ports;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.IdentityAccess;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "IdentityAccess")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ExternalAuthCallbackPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ExternalAuthCallbackPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Google_SignIn_Provisions_User_And_Exchange_Code_Works()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var google = scope.ServiceProvider.GetRequiredService<IGoogleOAuthPort>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var providerUserId = Guid.NewGuid().ToString("N");
        var email = $"google-{Guid.NewGuid():N}@containers.test";
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, providerUserId),
            new Claim(ClaimTypes.Email, email)
        ], "Google"));

        var signIn = await google.SignInOrProvisionAsync(principal, CancellationToken.None);
        Assert.True(signIn.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(signIn.Value!.AccessToken));

        var appUser = await userManager.FindByEmailAsync(email);
        Assert.NotNull(appUser);
        Assert.True(await db.RefreshTokens.AsNoTracking().AnyAsync(x => x.UserId == appUser!.Id));

        var code = await google.CreateExchangeCodeAsync(signIn.Value, CancellationToken.None);
        var payload = await google.ConsumeExchangeCodeAsync(code, CancellationToken.None);
        Assert.NotNull(payload);
        Assert.Equal(signIn.Value.AccessToken, payload!.AccessToken);

        var secondSignIn = await google.SignInOrProvisionAsync(principal, CancellationToken.None);
        Assert.True(secondSignIn.IsSuccess);
    }
}
