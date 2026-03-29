using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Marketplace.Infrastructure.Identity.Services;

/// <summary>Реалізація <see cref="ITokenPort"/> — JWT та токени Identity.</summary>
public class TokenService : ITokenPort
{
    private readonly JwtOptions _options;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IOptions<JwtOptions> options, UserManager<ApplicationUser> userManager)
    {
        _options = options.Value;
        _userManager = userManager;
    }

    public AuthToken GenerateAccessToken(IdentityUserId userId, string email, IList<string> roles)
    {
        var key = GetSigningKey();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return AuthToken.Create(token, TimeSpan.FromMinutes(_options.AccessTokenMinutes));
    }

    public RefreshToken GenerateRefreshToken() =>
        RefreshToken.Create(GenerateSecureToken(), _options.RefreshTokenDays);

    public Marketplace.Application.Auth.Ports.TokenValidationResult? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, GetValidationParameters(), out var validated);
            var jwt = (JwtSecurityToken)validated;
            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null || !Guid.TryParse(sub, out var uid))
                return null;

            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return new Marketplace.Application.Auth.Ports.TokenValidationResult(
                IdentityUserId.From(uid), email, roles, jwt.ValidTo);
        }
        catch
        {
            return null;
        }
    }

    public string GenerateEmailConfirmationToken(IdentityUserId userId, string email)
    {
        var user = _userManager.FindByIdAsync(userId.Value.ToString()).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("User not found for email confirmation token.");
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email does not match the user account.");
        return _userManager.GenerateEmailConfirmationTokenAsync(user).GetAwaiter().GetResult();
    }

    public string GeneratePasswordResetToken(IdentityUserId userId, string email)
    {
        var user = _userManager.FindByIdAsync(userId.Value.ToString()).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("User not found for password reset token.");
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email does not match the user account.");
        return _userManager.GeneratePasswordResetTokenAsync(user).GetAwaiter().GetResult();
    }

    private SymmetricSecurityKey GetSigningKey() => JwtParameterHelper.CreateSigningKey(_options);

    private TokenValidationParameters GetValidationParameters() =>
        JwtParameterHelper.CreateValidationParameters(_options);

    private static string GenerateSecureToken()
    {
        var buffer = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
