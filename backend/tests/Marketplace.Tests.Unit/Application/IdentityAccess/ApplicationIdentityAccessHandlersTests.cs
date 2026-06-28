using Marketplace.Application.Auth.Commands.Login;
using Marketplace.Application.Auth.Commands.Logout;
using Marketplace.Application.Auth.Commands.RefreshToken;
using Marketplace.Application.Auth.Commands.Register;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Users.Commands.AssignUserRole;
using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Tests;

[Trait("Suite", "IdentityAccess")]
public class ApplicationIdentityAccessHandlersTests
{
    [Fact]
    public async Task LoginHandler_Returns_Success_When_Port_Returns_Tokens()
    {
        var auth = new FakeAuthenticationPort();
        var sut = new LoginCommandHandler(auth);

        var result = await sut.Handle(new LoginCommand("user@example.com", "StrongPass1!"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task RefreshTokenHandler_Rejects_Empty_Token()
    {
        var sut = new RefreshTokenCommandHandler(new FakeAuthenticationPort());
        var result = await sut.Handle(new RefreshTokenCommand(" "), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Contains("required", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogoutHandler_Calls_Auth_Port()
    {
        var auth = new FakeAuthenticationPort();
        var sut = new LogoutCommandHandler(auth);

        var result = await sut.Handle(new LogoutCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, auth.LogoutCalls);
    }

    [Fact]
    public async Task RegisterHandler_Creates_Domain_User_And_Returns_Tokens()
    {
        var auth = new FakeAuthenticationPort();
        var users = new InMemoryUserRepository();
        var sut = new RegisterCommandHandler(auth, users, new NoopEmailPort(), null, new FakeTokenPort());

        var result = await sut.Handle(new RegisterCommand("u@e.com", "StrongPass1!", "John Doe"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(users.AddedUsers);
    }

    [Fact]
    public async Task AssignUserRoleHandler_Invalidates_Cache_On_Success()
    {
        var auth = new FakeAuthenticationPort();
        var cache = new SpyCachePort();
        var userId = Guid.NewGuid();
        var sut = new AssignUserRoleCommandHandler(auth, cache);

        var result = await sut.Handle(new AssignUserRoleCommand(userId, UserRole.Admin), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(cache.RemovedKeys, x => x.Contains(userId.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeAuthenticationPort : IAuthenticationPort
    {
        public bool RequireConfirmedEmail => false;
        public int LogoutCalls { get; private set; }

        public Task<Result<AuthTokens>> RegisterAsync(IdentityUserId identityId, Email email, UserName userName, string password, string? phoneNumber = null, CancellationToken ct = default)
            => Task.FromResult(Result<AuthTokens>.Success(BuildTokens()));

        public Task<Result<AuthTokens>> LoginAsync(Email email, string password, string? twoFactorCode = null, CancellationToken ct = default)
            => Task.FromResult(Result<AuthTokens>.Success(BuildTokens()));

        public Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
            => Task.FromResult(Result<AuthTokens>.Success(BuildTokens()));

        public Task<Result> LogoutAsync(IdentityUserId userId, CancellationToken ct = default)
        {
            LogoutCalls++;
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ConfirmEmailAsync(Email email, string token, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<string>> GeneratePasswordResetTokenAsync(Email email, CancellationToken ct = default) => Task.FromResult(Result<string>.Success("t"));
        public Task<Result> ResetPasswordAsync(Email email, string resetToken, string newPassword, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> DeleteAccountAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> SendEmailTwoFactorCodeAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> EnableEmailTwoFactorAsync(IdentityUserId userId, string code, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> DisableEmailTwoFactorAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<string>> GenerateTelegramLinkCodeAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result<string>.Success("abc"));
        public Task<Result> LinkTelegramAccountAsync(string linkCode, string chatId, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> SendTelegramTwoFactorCodeAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> EnableTelegramTwoFactorAsync(IdentityUserId userId, string code, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> DisableTelegramTwoFactorAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<TwoFactorStatusDto>> GetTwoFactorStatusAsync(IdentityUserId userId, CancellationToken ct = default) => Task.FromResult(Result<TwoFactorStatusDto>.Success(new TwoFactorStatusDto(false, false, false)));
        public Task<Result> AssignUserRoleAsync(IdentityUserId userId, UserRole role, CancellationToken ct = default) => Task.FromResult(Result.Success());

        private static AuthTokens BuildTokens()
            => AuthTokens.Create(AuthToken.Create("access", TimeSpan.FromMinutes(10)), RefreshToken.Create("refresh", 30));
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> AddedUsers { get; } = [];

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdentityIdAsync(IdentityUserId identityId, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<User>>([]);
        public Task<IReadOnlyList<User>> SearchByUserNameAsync(string userName, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<User>>([]);
        public Task AddAsync(User user, CancellationToken ct = default)
        {
            AddedUsers.Add(user);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopEmailPort : IEmailPort
    {
        public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeTokenPort : ITokenPort
    {
        public AuthToken GenerateAccessToken(IdentityUserId userId, string email, IList<string> roles) => AuthToken.Create("access", TimeSpan.FromMinutes(10));
        public RefreshToken GenerateRefreshToken() => RefreshToken.Create("refresh", 30);
        public Marketplace.Application.Auth.Ports.TokenValidationResult? ValidateToken(string token) => null;
        public string GenerateEmailConfirmationToken(IdentityUserId userId, string email) => "confirm";
        public string GeneratePasswordResetToken(IdentityUserId userId, string email) => "reset";
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        public List<string> RemovedKeys { get; } = [];
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
