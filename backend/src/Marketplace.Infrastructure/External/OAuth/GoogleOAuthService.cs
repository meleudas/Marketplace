using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Caching;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Security;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.Infrastructure.External.OAuth;

public sealed class GoogleOAuthService
{
    private const string GoogleProvider = "Google";
    private const string StateKeyPrefix = "oauth:google:state:";
    private const string CodeKeyPrefix = "oauth:google:code:";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenPort _tokenPort;
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cacheService;

    public GoogleOAuthService(
        UserManager<ApplicationUser> userManager,
        ITokenPort tokenPort,
        IUserRepository userRepository,
        ApplicationDbContext db,
        ICacheService cacheService)
    {
        _userManager = userManager;
        _tokenPort = tokenPort;
        _userRepository = userRepository;
        _db = db;
        _cacheService = cacheService;
    }

    public async Task<string> CreateAuthStateAsync(string returnPath, CancellationToken ct = default)
    {
        var state = Guid.NewGuid().ToString("N");
        await _cacheService.SetAsync(StateKeyPrefix + state, returnPath, TimeSpan.FromMinutes(5), ct);
        return state;
    }

    public async Task<string?> ConsumeAuthStateAsync(string state, CancellationToken ct = default)
    {
        var key = StateKeyPrefix + state;
        var returnPath = await _cacheService.GetAsync<string>(key, ct);
        if (returnPath is null)
            return null;

        await _cacheService.RemoveAsync(key, ct);
        return returnPath;
    }

    public async Task<Result<AuthTokensDto>> SignInOrProvisionAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken ct = default)
    {
        try
        {
            var providerUserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email))
                return Result<AuthTokensDto>.Failure("Google profile did not provide required claims.");

            var appUser = await _userManager.FindByLoginAsync(GoogleProvider, providerUserId);

            if (appUser is null)
            {
                appUser = await _userManager.FindByEmailAsync(email);

                if (appUser is not null)
                {
                    var linkResult = await _userManager.AddLoginAsync(
                        appUser,
                        new UserLoginInfo(GoogleProvider, providerUserId, GoogleProvider));

                    if (!linkResult.Succeeded)
                        return Result<AuthTokensDto>.Failure(string.Join(" ", linkResult.Errors.Select(e => e.Description)));
                }
            }

            if (appUser is null)
            {
                var identityId = Guid.NewGuid();
                var userName = await GenerateUniqueUserNameAsync(email);

                appUser = new ApplicationUser
                {
                    Id = identityId,
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    IsDeleted = false
                };

                var createResult = await _userManager.CreateAsync(appUser);
                if (!createResult.Succeeded)
                    return Result<AuthTokensDto>.Failure(string.Join(" ", createResult.Errors.Select(e => e.Description)));

                var loginResult = await _userManager.AddLoginAsync(
                    appUser,
                    new UserLoginInfo(GoogleProvider, providerUserId, GoogleProvider));
                if (!loginResult.Succeeded)
                    return Result<AuthTokensDto>.Failure(string.Join(" ", loginResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                if (!roleResult.Succeeded)
                    return Result<AuthTokensDto>.Failure(string.Join(" ", roleResult.Errors.Select(e => e.Description)));
            }

            if (appUser.IsDeleted)
                return Result<AuthTokensDto>.Failure("User account is disabled.");

            await EnsureDomainUserAsync(appUser, ct);

            var roles = await _userManager.GetRolesAsync(appUser);
            var access = _tokenPort.GenerateAccessToken(IdentityUserId.From(appUser.Id), appUser.Email ?? string.Empty, roles);
            var refresh = _tokenPort.GenerateRefreshToken();

            _db.RefreshTokens.Add(new RefreshTokenRecord
            {
                Id = Guid.NewGuid(),
                UserId = appUser.Id,
                TokenHash = TokenHasher.Sha256Hex(refresh.Token),
                ExpiresAt = refresh.ExpiresAt,
                CreatedAt = refresh.CreatedAt
            });
            await _db.SaveChangesAsync(ct);

            var dto = new AuthTokensDto(
                access.Value,
                refresh.Token,
                access.ExpiresAt,
                refresh.ExpiresAt);

            return Result<AuthTokensDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<AuthTokensDto>.Failure($"Google sign-in failed: {ex.Message}");
        }
    }

    public async Task<string> CreateExchangeCodeAsync(AuthTokensDto tokens, CancellationToken ct = default)
    {
        var code = Guid.NewGuid().ToString("N");
        await _cacheService.SetAsync(CodeKeyPrefix + code, new GoogleExchangePayload(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt), TimeSpan.FromMinutes(2), ct);
        return code;
    }

    public async Task<GoogleExchangePayload?> ConsumeExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var key = CodeKeyPrefix + code;
        var payload = await _cacheService.GetAsync<GoogleExchangePayload>(key, ct);
        if (payload is null)
            return null;

        await _cacheService.RemoveAsync(key, ct);
        return payload;
    }

    private async Task EnsureDomainUserAsync(ApplicationUser appUser, CancellationToken ct)
    {
        var identityId = IdentityUserId.From(appUser.Id);
        var existingDomain = await _userRepository.GetByIdentityIdAsync(identityId, ct);
        if (existingDomain is not null)
            return;

        var display = appUser.UserName ?? "user";
        var split = display.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = split.Length > 0 ? split[0] : display;
        var lastName = split.Length > 1 ? string.Join(' ', split.Skip(1)) : "-";

        var user = User.Create(identityId, firstName, lastName);
        user.Verify();
        await _userRepository.AddAsync(user, ct);
    }

    private async Task<string> GenerateUniqueUserNameAsync(string email)
    {
        var baseName = email.Split('@', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        if (baseName.Length < 3)
            baseName = "user" + Guid.NewGuid().ToString("N")[..6];

        baseName = new string(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.').ToArray());
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "user" + Guid.NewGuid().ToString("N")[..6];

        if (baseName.Length > 50)
            baseName = baseName[..50];

        for (var i = 0; i < 50; i++)
        {
            var candidate = i == 0 ? baseName : $"{baseName}_{i}";
            if (candidate.Length > 50)
                candidate = candidate[..50];

            var exists = await _userManager.FindByNameAsync(candidate);
            if (exists is null)
                return candidate;
        }

        return "user" + Guid.NewGuid().ToString("N")[..8];
    }

    public sealed record GoogleExchangePayload(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        DateTime RefreshTokenExpiresAt);
}
