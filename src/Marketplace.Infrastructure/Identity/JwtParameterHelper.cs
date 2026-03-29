using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Marketplace.Infrastructure.Identity;

internal static class JwtParameterHelper
{
    public static SymmetricSecurityKey CreateSigningKey(JwtOptions options)
    {
        var bytes = Encoding.UTF8.GetBytes(options.SecretKey);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 bytes for HS256.");
        return new SymmetricSecurityKey(bytes);
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions options) =>
        new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = CreateSigningKey(options),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
}
