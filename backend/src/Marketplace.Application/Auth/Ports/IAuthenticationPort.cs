using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Application.Auth.Ports
{
    public interface IAuthenticationPort
    {
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
    }
}
