using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Application.Auth.Ports
{
    public interface IAuthenticationPort
    {
        bool RequireConfirmedEmail { get; }

        Task<Result<AuthTokens>> RegisterAsync(
            IdentityUserId identityId,
            Email email,
            UserName userName,
            string password,
            string? phoneNumber = null,
            CancellationToken ct = default);

        Task<Result<AuthTokens>> LoginAsync(
            Email email,
            string password,
            string? twoFactorCode = null,
            CancellationToken ct = default);

        Task<Result<AuthTokens>> RefreshTokenAsync(
            string refreshToken,
            CancellationToken ct = default);

        Task<Result> LogoutAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result> ConfirmEmailAsync(Email email, string token, CancellationToken ct = default);

        Task<Result<string>> GeneratePasswordResetTokenAsync(Email email, CancellationToken ct = default);

        Task<Result> ResetPasswordAsync(
            Email email,
            string resetToken,
            string newPassword,
            CancellationToken ct = default);

        Task<Result> DeleteAccountAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result> SendEmailTwoFactorCodeAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result> EnableEmailTwoFactorAsync(IdentityUserId userId, string code, CancellationToken ct = default);

        Task<Result> DisableEmailTwoFactorAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result<string>> GenerateTelegramLinkCodeAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result> LinkTelegramAccountAsync(string linkCode, string chatId, CancellationToken ct = default);

        Task<Result> SendTelegramTwoFactorCodeAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result> EnableTelegramTwoFactorAsync(IdentityUserId userId, string code, CancellationToken ct = default);

        Task<Result> DisableTelegramTwoFactorAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result<TwoFactorStatusDto>> GetTwoFactorStatusAsync(IdentityUserId userId, CancellationToken ct = default);

        Task<Result> AssignUserRoleAsync(IdentityUserId userId, UserRole role, CancellationToken ct = default);
    }
}
