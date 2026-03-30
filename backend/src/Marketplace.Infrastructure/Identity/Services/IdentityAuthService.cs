using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Security;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Identity.Services;

/// <summary>–Â‡Î≥Á‡ˆ≥ˇ <see cref="IAuthenticationPort"/> Ì‡ ·‡Á≥ ASP.NET Identity.</summary>
public class IdentityAuthService : IAuthenticationPort
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ITokenPort _tokenPort;
    private readonly IdentityUserService _identityUserService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailPort _emailPort;

    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ITokenPort tokenPort,
        IdentityUserService identityUserService,
        IUserRepository userRepository,
        IEmailPort emailPort)
    {
        _userManager = userManager;
        _db = db;
        _tokenPort = tokenPort;
        _identityUserService = identityUserService;
        _userRepository = userRepository;
        _emailPort = emailPort;
    }

    public async Task<Result<AuthTokens>> RegisterAsync(
        IdentityUserId identityId,
        Email email,
        UserName userName,
        string password,
        string? phoneNumber = null,
        CancellationToken ct = default)
    {
        var appUser = _identityUserService.CreateForRegistration(identityId, email, userName, phoneNumber);

        var create = await _userManager.CreateAsync(appUser, password);
        if (!create.Succeeded)
            return Result<AuthTokens>.Failure(string.Join(" ", create.Errors.Select(e => e.Description)));

        var inRole = await _userManager.AddToRoleAsync(appUser, "User");
        if (!inRole.Succeeded)
            return Result<AuthTokens>.Failure(string.Join(" ", inRole.Errors.Select(e => e.Description)));

        return await IssueTokensAsync(appUser, ct);
    }

    public async Task<Result<AuthTokens>> LoginAsync(Email email, string password, string? twoFactorCode = null, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email.Value);
        if (appUser is null || appUser.IsDeleted)
            return Result<AuthTokens>.Failure("Invalid email or password.");

        if (!await _userManager.CheckPasswordAsync(appUser, password))
            return Result<AuthTokens>.Failure("Invalid email or password.");

        if (appUser.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                var sendCode = await SendTwoFactorCodeInternalAsync(appUser, ct);
                if (sendCode.IsFailure)
                    return Result<AuthTokens>.Failure(sendCode.Error ?? "Failed to send 2FA code.");

                return Result<AuthTokens>.Failure("2FA code required. A verification code was sent to your email.");
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                appUser,
                TokenOptions.DefaultEmailProvider,
                twoFactorCode);

            if (!isValid)
                return Result<AuthTokens>.Failure("Invalid 2FA code.");
        }

        var domainUser = await _userRepository.GetByIdentityIdAsync(IdentityUserId.From(appUser.Id), ct);
        if (domainUser is not null)
        {
            domainUser.UpdateLastLogin();
            await _userRepository.UpdateAsync(domainUser, ct);
        }

        return await IssueTokensAsync(appUser, ct);
    }

    public async Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = TokenHasher.Sha256Hex(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt < DateTime.UtcNow)
            return Result<AuthTokens>.Failure("Invalid or expired refresh token.");

        var appUser = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result<AuthTokens>.Failure("User no longer exists.");

        stored.RevokedAt = DateTime.UtcNow;

        var newRefresh = _tokenPort.GenerateRefreshToken();
        var newRecord = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = stored.UserId,
            TokenHash = TokenHasher.Sha256Hex(newRefresh.Token),
            ExpiresAt = newRefresh.ExpiresAt,
            CreatedAt = newRefresh.CreatedAt,
            ReplacedByTokenHash = null
        };
        stored.ReplacedByTokenHash = newRecord.TokenHash;
        _db.RefreshTokens.Add(newRecord);

        var roles = await _userManager.GetRolesAsync(appUser);
        var identityId = IdentityUserId.From(appUser.Id);
        var access = _tokenPort.GenerateAccessToken(identityId, appUser.Email ?? string.Empty, roles);

        await _db.SaveChangesAsync(ct);
        return Result<AuthTokens>.Success(AuthTokens.Create(access, newRefresh));
    }

    public async Task<Result> LogoutAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var active = await _db.RefreshTokens
            .Where(x => x.UserId == userId.Value && x.RevokedAt == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var t in active)
            t.RevokedAt = now;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(Email email, string token, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email.Value);
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("Invalid confirmation token.");

        var r = await _userManager.ConfirmEmailAsync(appUser, token);
        if (!r.Succeeded)
            return Result.Failure(string.Join(" ", r.Errors.Select(e => e.Description)));

        var domainUser = await _userRepository.GetByIdentityIdAsync(IdentityUserId.From(appUser.Id), ct);
        if (domainUser is not null)
        {
            domainUser.Verify();
            await _userRepository.UpdateAsync(domainUser, ct);
        }

        return Result.Success();
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(Email email, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email.Value);
        if (appUser is null || appUser.IsDeleted)
            return Result<string>.Failure("User not found");

        var token = await _userManager.GeneratePasswordResetTokenAsync(appUser);
        return Result<string>.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(
        Email email,
        string resetToken,
        string newPassword,
        CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email.Value);
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("Invalid reset token.");

        var r = await _userManager.ResetPasswordAsync(appUser, resetToken, newPassword);
        if (!r.Succeeded)
            return Result.Failure(string.Join(" ", r.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> DeleteAccountAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        await LogoutAsync(userId, ct);

        var domainUser = await _userRepository.GetByIdentityIdAsync(userId, ct);
        if (domainUser is not null && !domainUser.IsDeleted)
        {
            domainUser.SoftDelete();
            await _userRepository.UpdateAsync(domainUser, ct);
        }

        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null)
            return Result.Success();

        appUser.IsDeleted = true;
        var update = await _userManager.UpdateAsync(appUser);
        if (!update.Succeeded)
            return Result.Failure(string.Join(" ", update.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> SendEmailTwoFactorCodeAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        return await SendTwoFactorCodeInternalAsync(appUser, ct);
    }

    public async Task<Result> EnableEmailTwoFactorAsync(IdentityUserId userId, string code, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            appUser,
            TokenOptions.DefaultEmailProvider,
            code);

        if (!isValid)
            return Result.Failure("Invalid 2FA code.");

        appUser.TwoFactorEnabled = true;
        var update = await _userManager.UpdateAsync(appUser);
        if (!update.Succeeded)
            return Result.Failure(string.Join(" ", update.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> DisableEmailTwoFactorAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        appUser.TwoFactorEnabled = false;
        var update = await _userManager.UpdateAsync(appUser);
        if (!update.Succeeded)
            return Result.Failure(string.Join(" ", update.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    private async Task<Result<AuthTokens>> IssueTokensAsync(ApplicationUser appUser, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(appUser);
        var identityId = IdentityUserId.From(appUser.Id);
        var access = _tokenPort.GenerateAccessToken(identityId, appUser.Email ?? string.Empty, roles);
        var refresh = _tokenPort.GenerateRefreshToken();

        var record = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = appUser.Id,
            TokenHash = TokenHasher.Sha256Hex(refresh.Token),
            ExpiresAt = refresh.ExpiresAt,
            CreatedAt = refresh.CreatedAt
        };
        _db.RefreshTokens.Add(record);
        await _db.SaveChangesAsync(ct);

        return Result<AuthTokens>.Success(AuthTokens.Create(access, refresh));
    }

    private async Task<Result> SendTwoFactorCodeInternalAsync(ApplicationUser appUser, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appUser.Email))
            return Result.Failure("User email is not configured.");

        var code = await _userManager.GenerateTwoFactorTokenAsync(appUser, TokenOptions.DefaultEmailProvider);
        await _emailPort.SendTwoFactorCodeEmailAsync(appUser.Email, code, ct);
        return Result.Success();
    }
}
