using System.Security.Claims;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Auth.Ports;

public interface IGoogleOAuthPort
{
    Task<string> CreateAuthStateAsync(string returnPath, CancellationToken ct = default);
    Task<string?> ConsumeAuthStateAsync(string state, CancellationToken ct = default);
    Task<Result<AuthTokensDto>> SignInOrProvisionAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task<string> CreateExchangeCodeAsync(AuthTokensDto tokens, CancellationToken ct = default);
    Task<GoogleOAuthExchangePayload?> ConsumeExchangeCodeAsync(string code, CancellationToken ct = default);
}

public sealed record GoogleOAuthExchangePayload(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
