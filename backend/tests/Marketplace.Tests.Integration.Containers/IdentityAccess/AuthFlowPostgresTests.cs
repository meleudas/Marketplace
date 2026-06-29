using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Users.ValueObjects;
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
public sealed class AuthFlowPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public AuthFlowPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Register_Login_Refresh_And_Logout_Work_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationPort>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var identityId = Guid.NewGuid();
        var email = Email.Create($"auth-{Guid.NewGuid():N}@containers.test");
        const string password = "StrongPass1!AB";

        var register = await auth.RegisterAsync(identityId, email, UserName.Create($"user_{Guid.NewGuid():N}"[..20]), password, null, CancellationToken.None);
        Assert.True(register.IsSuccess);

        var login = await auth.LoginAsync(email, password, null, CancellationToken.None);
        Assert.True(login.IsSuccess);
        Assert.NotNull(login.Value);

        var refreshed = await auth.RefreshTokenAsync(login.Value!.RefreshToken.Token, CancellationToken.None);
        Assert.True(refreshed.IsSuccess);

        await auth.LogoutAsync(identityId, CancellationToken.None);
        var active = await db.RefreshTokens
            .Where(x => x.UserId == identityId && x.RevokedAt == null)
            .ToListAsync(CancellationToken.None);
        Assert.Empty(active);
    }

    [Fact]
    public async Task Soft_Deleted_User_Cannot_Login()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationPort>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var identityId = Guid.NewGuid();
        var email = Email.Create($"deleted-{Guid.NewGuid():N}@containers.test");
        const string password = "StrongPass1!AB";

        _ = await auth.RegisterAsync(identityId, email, UserName.Create($"del_{Guid.NewGuid():N}"[..20]), password, null, CancellationToken.None);
        var user = await userManager.FindByIdAsync(identityId.ToString());
        Assert.NotNull(user);
        user!.IsDeleted = true;
        await userManager.UpdateAsync(user);

        var login = await auth.LoginAsync(email, password, null, CancellationToken.None);
        Assert.True(login.IsFailure);
    }
}
