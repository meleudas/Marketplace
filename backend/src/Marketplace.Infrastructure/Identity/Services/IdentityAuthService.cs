using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Security;
using Marketplace.Infrastructure.External.Telegram;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Marketplace.Infrastructure.Identity.Services;

/// <summary>????????? <see cref="IAuthenticationPort"/> ?? ??? ASP.NET Identity.</summary>
public class IdentityAuthService : IAuthenticationPort
{
    public bool RequireConfirmedEmail => _userManager.Options.SignIn.RequireConfirmedEmail;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ITokenPort _tokenPort;
    private readonly IdentityUserService _identityUserService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailPort _emailPort;
    private readonly ITelegramPort _telegramPort;
    private readonly ITelegramLinkCodeStore _telegramLinkCodeStore;
    private readonly TelegramOptions _telegramOptions;

    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ITokenPort tokenPort,
        IdentityUserService identityUserService,
        IUserRepository userRepository,
        IEmailPort emailPort,
        ITelegramPort telegramPort,
        ITelegramLinkCodeStore telegramLinkCodeStore,
        IOptions<TelegramOptions> telegramOptions)
    {
        _userManager = userManager;
        _db = db;
        _tokenPort = tokenPort;
        _identityUserService = identityUserService;
        _userRepository = userRepository;
        _emailPort = emailPort;
        _telegramPort = telegramPort;
        _telegramLinkCodeStore = telegramLinkCodeStore;
        _telegramOptions = telegramOptions.Value;
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

        if (RequireConfirmedEmail && !appUser.EmailConfirmed)
            return Result<AuthTokens>.Success(null!);

        return await IssueTokensAsync(appUser, ct);
    }

    public async Task<Result<AuthTokens>> LoginAsync(Email email, string password, string? twoFactorCode = null, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email.Value);
        if (appUser is null || appUser.IsDeleted)
            return Result<AuthTokens>.Failure("Invalid email or password.");

        if (RequireConfirmedEmail && !appUser.EmailConfirmed)
            return Result<AuthTokens>.Failure("Please confirm your email before login.");

        if (!await _userManager.CheckPasswordAsync(appUser, password))
            return Result<AuthTokens>.Failure("Invalid email or password.");

        if (appUser.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                var sendCode = appUser.TelegramTwoFactorEnabled
                    ? await SendTelegramTwoFactorCodeInternalAsync(appUser, ct)
                    : await SendEmailTwoFactorCodeInternalAsync(appUser, ct);
                if (sendCode.IsFailure)
                    return Result<AuthTokens>.Failure(sendCode.Error ?? "Failed to send 2FA code.");

                var channelMessage = appUser.TelegramTwoFactorEnabled
                    ? "A verification code was sent to your Telegram."
                    : "A verification code was sent to your email.";
                return Result<AuthTokens>.Failure($"2FA code required. {channelMessage}");
            }

            var isValid = appUser.TelegramTwoFactorEnabled
                ? await _userManager.VerifyTwoFactorTokenAsync(appUser, TokenOptions.DefaultPhoneProvider, twoFactorCode)
                : await _userManager.VerifyTwoFactorTokenAsync(appUser, TokenOptions.DefaultEmailProvider, twoFactorCode);

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

        if (RequireConfirmedEmail && !appUser.EmailConfirmed)
            return Result<AuthTokens>.Failure("Please confirm your email before login.");

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
        return await SendEmailTwoFactorCodeInternalAsync(appUser, ct);
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

    public async Task<Result<string>> GenerateTelegramLinkCodeAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result<string>.Failure("User no longer exists.");

        var code = GenerateLinkCode();
        var ttlMinutes = Math.Clamp(_telegramOptions.LinkCodeTtlMinutes, 1, 60);
        await _telegramLinkCodeStore.StoreAsync(code, appUser.Id, TimeSpan.FromMinutes(ttlMinutes), ct);
        return Result<string>.Success(code);
    }

    public async Task<Result> LinkTelegramAccountAsync(string linkCode, string chatId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(linkCode) || string.IsNullOrWhiteSpace(chatId))
            return Result.Failure("Link code and chat id are required.");

        var userId = await _telegramLinkCodeStore.TakeAsync(linkCode, ct);
        if (userId is null)
            return Result.Failure("Link code is invalid or expired.");

        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        appUser.TelegramChatId = chatId;
        appUser.TelegramLinkedAtUtc = DateTime.UtcNow;
        var update = await _userManager.UpdateAsync(appUser);
        if (!update.Succeeded)
            return Result.Failure(string.Join(" ", update.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> SendTelegramTwoFactorCodeAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        return await SendTelegramTwoFactorCodeInternalAsync(appUser, ct);
    }

    public async Task<Result> EnableTelegramTwoFactorAsync(IdentityUserId userId, string code, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");
        if (string.IsNullOrWhiteSpace(appUser.TelegramChatId))
            return Result.Failure("Telegram account is not linked.");

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            appUser,
            TokenOptions.DefaultPhoneProvider,
            code);

        if (!isValid)
            return Result.Failure("Invalid 2FA code.");

        appUser.TelegramTwoFactorEnabled = true;
        appUser.TwoFactorEnabled = true;
        var update = await _userManager.UpdateAsync(appUser);
        if (!update.Succeeded)
            return Result.Failure(string.Join(" ", update.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> DisableTelegramTwoFactorAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        appUser.TelegramTwoFactorEnabled = false;
        appUser.TwoFactorEnabled = false;

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

        appUser.TwoFactorEnabled = appUser.TelegramTwoFactorEnabled;
        var update = await _userManager.UpdateAsync(appUser);
        if (!update.Succeeded)
            return Result.Failure(string.Join(" ", update.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result<TwoFactorStatusDto>> GetTwoFactorStatusAsync(IdentityUserId userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result<TwoFactorStatusDto>.Failure("User no longer exists.");

        var dto = new TwoFactorStatusDto(
            appUser.TwoFactorEnabled,
            appUser.TelegramTwoFactorEnabled,
            !string.IsNullOrWhiteSpace(appUser.TelegramChatId));

        return Result<TwoFactorStatusDto>.Success(dto);
    }

    public async Task<Result> AssignUserRoleAsync(IdentityUserId userId, UserRole role, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (appUser is null || appUser.IsDeleted)
            return Result.Failure("User no longer exists.");

        var managedRoles = new[] { nameof(UserRole.Buyer), nameof(UserRole.Seller), nameof(UserRole.Moderator), nameof(UserRole.Admin) };
        var currentRoles = await _userManager.GetRolesAsync(appUser);
        var rolesToRemove = currentRoles.Where(r => managedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
        if (rolesToRemove.Count > 0)
        {
            var remove = await _userManager.RemoveFromRolesAsync(appUser, rolesToRemove);
            if (!remove.Succeeded)
                return Result.Failure(string.Join(" ", remove.Errors.Select(e => e.Description)));
        }

        var targetRoleName = role.ToString();
        var add = await _userManager.AddToRoleAsync(appUser, targetRoleName);
        if (!add.Succeeded)
            return Result.Failure(string.Join(" ", add.Errors.Select(e => e.Description)));

        var domainUser = await _userRepository.GetByIdentityIdAsync(userId, ct);
        if (domainUser is not null)
        {
            domainUser.SetRole(role);
            await _userRepository.UpdateAsync(domainUser, ct);
        }

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

    private async Task<Result> SendEmailTwoFactorCodeInternalAsync(ApplicationUser appUser, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appUser.Email))
            return Result.Failure("User email is not configured.");

        var code = await _userManager.GenerateTwoFactorTokenAsync(appUser, TokenOptions.DefaultEmailProvider);
        await _emailPort.SendTwoFactorCodeEmailAsync(appUser.Email, code, ct);
        return Result.Success();
    }

    private async Task<Result> SendTelegramTwoFactorCodeInternalAsync(ApplicationUser appUser, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appUser.TelegramChatId))
            return Result.Failure("Telegram account is not linked.");

        var code = await _userManager.GenerateTwoFactorTokenAsync(appUser, TokenOptions.DefaultPhoneProvider);
        await _telegramPort.SendMessageAsync(appUser.TelegramChatId, $"Your Marketplace verification code: {code}", ct);
        return Result.Success();
    }

    private static string GenerateLinkCode()
    {
        Span<byte> bytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

}
