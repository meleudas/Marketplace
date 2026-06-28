using Marketplace.Application.Auth.Options;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.External.Telegram;
using Marketplace.Infrastructure.Identity;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Services;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Entities;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "IdentityAccess")]
public class IntegrationIdentityAccessSqliteTests
{
    [Fact]
    public async Task Register_Login_Refresh_Logout_Flow_Works_With_Rotation()
    {
        await using var fixture = await CreateFixtureAsync(requireConfirmedEmail: false);
        var service = fixture.AuthService;
        var identityId = IdentityUserId.New();
        var email = Email.Create("flow@test.local");
        var password = "StrongPass1!AB";

        var register = await service.RegisterAsync(identityId, email, UserName.Create("flow_user"), password, null, CancellationToken.None);
        Assert.True(register.IsSuccess);
        Assert.NotNull(register.Value);

        var login = await service.LoginAsync(email, password, null, CancellationToken.None);
        Assert.True(login.IsSuccess);
        Assert.NotNull(login.Value);

        var refreshed = await service.RefreshTokenAsync(login.Value!.RefreshToken.Token, CancellationToken.None);
        Assert.True(refreshed.IsSuccess);
        Assert.NotNull(refreshed.Value);

        await service.LogoutAsync(identityId, CancellationToken.None);

        var active = await fixture.Db.RefreshTokens
            .Where(x => x.UserId == identityId.Value && x.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(active);
    }

    [Fact]
    public async Task Replay_RefreshToken_Is_Detected_And_Sessions_Are_Revoked()
    {
        await using var fixture = await CreateFixtureAsync(requireConfirmedEmail: false);
        var service = fixture.AuthService;
        var identityId = IdentityUserId.New();
        var email = Email.Create("replay@test.local");
        var password = "StrongPass1!AB";

        _ = await service.RegisterAsync(identityId, email, UserName.Create("replay_user"), password, null, CancellationToken.None);
        var login = await service.LoginAsync(email, password, null, CancellationToken.None);
        Assert.True(login.IsSuccess);
        Assert.NotNull(login.Value);

        var firstRefresh = await service.RefreshTokenAsync(login.Value!.RefreshToken.Token, CancellationToken.None);
        Assert.True(firstRefresh.IsSuccess);

        var replay = await service.RefreshTokenAsync(login.Value.RefreshToken.Token, CancellationToken.None);
        Assert.True(replay.IsFailure);
        Assert.Contains("replay", replay.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var active = await fixture.Db.RefreshTokens
            .Where(x => x.UserId == identityId.Value && x.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(active);
    }

    [Fact]
    public async Task TwoFactor_Login_Requires_Valid_Code()
    {
        await using var fixture = await CreateFixtureAsync(requireConfirmedEmail: false);
        var service = fixture.AuthService;
        var identityId = IdentityUserId.New();
        var email = Email.Create("twofactor@test.local");
        var password = "StrongPass1!AB";

        _ = await service.RegisterAsync(identityId, email, UserName.Create("twofactor_user"), password, null, CancellationToken.None);
        var sendCode = await service.SendEmailTwoFactorCodeAsync(identityId, CancellationToken.None);
        Assert.True(sendCode.IsSuccess);

        var appUser = await fixture.UserManager.FindByIdAsync(identityId.Value.ToString());
        Assert.NotNull(appUser);
        var validCode = await fixture.UserManager.GenerateTwoFactorTokenAsync(appUser!, TokenOptions.DefaultEmailProvider);

        var enable = await service.EnableEmailTwoFactorAsync(identityId, validCode, CancellationToken.None);
        Assert.True(enable.IsSuccess);

        var required = await service.LoginAsync(email, password, null, CancellationToken.None);
        Assert.True(required.IsFailure);
        Assert.Contains("2FA code required", required.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var invalid = await service.LoginAsync(email, password, "111111", CancellationToken.None);
        Assert.True(invalid.IsFailure);

        var success = await service.LoginAsync(email, password, validCode, CancellationToken.None);
        Assert.True(success.IsSuccess);
        Assert.NotNull(success.Value);
    }

    [Fact]
    public async Task Production_Mode_Requires_Confirmed_Email()
    {
        await using var fixture = await CreateFixtureAsync(requireConfirmedEmail: true);
        var service = fixture.AuthService;
        var identityId = IdentityUserId.New();
        var email = Email.Create("prod@test.local");
        var password = "StrongPass1!AB";

        var register = await service.RegisterAsync(identityId, email, UserName.Create("prod_user"), password, null, CancellationToken.None);
        Assert.True(register.IsSuccess);
        Assert.Null(register.Value);

        var blockedLogin = await service.LoginAsync(email, password, null, CancellationToken.None);
        Assert.True(blockedLogin.IsFailure);
        Assert.Contains("confirm your email", blockedLogin.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPassword_Revokes_All_Refresh_Sessions()
    {
        await using var fixture = await CreateFixtureAsync(requireConfirmedEmail: false);
        var service = fixture.AuthService;
        var identityId = IdentityUserId.New();
        var email = Email.Create("reset@test.local");
        var oldPassword = "StrongPass1!AB";
        var newPassword = "NewStrongPass2!CD";

        _ = await service.RegisterAsync(identityId, email, UserName.Create("reset_user"), oldPassword, null, CancellationToken.None);
        var login = await service.LoginAsync(email, oldPassword, null, CancellationToken.None);
        Assert.True(login.IsSuccess);

        var resetToken = await service.GeneratePasswordResetTokenAsync(email, CancellationToken.None);
        Assert.True(resetToken.IsSuccess);

        var reset = await service.ResetPasswordAsync(email, resetToken.Value!, newPassword, CancellationToken.None);
        Assert.True(reset.IsSuccess);

        var active = await fixture.Db.RefreshTokens
            .Where(x => x.UserId == identityId.Value && x.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(active);

        var oldLogin = await service.LoginAsync(email, oldPassword, null, CancellationToken.None);
        Assert.True(oldLogin.IsFailure);
        var newLogin = await service.LoginAsync(email, newPassword, null, CancellationToken.None);
        Assert.True(newLogin.IsSuccess);
    }

    private static async Task<IdentityFixture> CreateFixtureAsync(bool requireConfirmedEmail)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.SignIn.RequireConfirmedEmail = requireConfirmedEmail;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(options =>
        {
            options.SecretKey = "IntegrationTestsJwtKey_AtLeast32Bytes!!";
            options.Issuer = "Marketplace.Tests";
            options.Audience = "Marketplace.Tests";
            options.AccessTokenMinutes = 10;
            options.RefreshTokenDays = 30;
        });
        services.Configure<TelegramOptions>(_ => { });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IdentityUserService>();
        services.AddScoped<ITokenPort, TokenService>();
        services.AddSingleton<ITelegramLinkCodeStore, InMemoryTelegramLinkCodeStore>();
        services.AddSingleton<INotificationDispatcher, NoopNotificationDispatcher>();
        services.AddScoped<IdentityAuthService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(nameof(Marketplace.Domain.Users.Enums.UserRole.Buyer)))
            await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Marketplace.Domain.Users.Enums.UserRole.Buyer)));

        return new IdentityFixture(
            connection,
            scope,
            db,
            scope.ServiceProvider.GetRequiredService<IdentityAuthService>(),
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    private sealed record IdentityFixture(
        SqliteConnection Connection,
        AsyncServiceScope Scope,
        ApplicationDbContext Db,
        IdentityAuthService AuthService,
        UserManager<ApplicationUser> UserManager) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Scope.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class NoopNotificationDispatcher : INotificationDispatcher
    {
        public Task EnqueueConfirmationEmailAsync(string to, string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnqueuePasswordResetEmailAsync(string to, string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnqueueTwoFactorEmailAsync(string to, string code, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnqueueTelegramMessageAsync(string chatId, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnqueueSmsAsync(string phoneNumber, string message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryTelegramLinkCodeStore : ITelegramLinkCodeStore
    {
        private readonly Dictionary<string, Guid> _items = new(StringComparer.OrdinalIgnoreCase);

        public Task StoreAsync(string code, Guid identityUserId, TimeSpan ttl, CancellationToken ct = default)
        {
            _items[code] = identityUserId;
            return Task.CompletedTask;
        }

        public Task<Guid?> TakeAsync(string code, CancellationToken ct = default)
        {
            if (!_items.TryGetValue(code, out var userId))
                return Task.FromResult<Guid?>(null);

            _items.Remove(code);
            return Task.FromResult<Guid?>(userId);
        }
    }
}
