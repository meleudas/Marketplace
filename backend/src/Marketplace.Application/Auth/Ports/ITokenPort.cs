using Marketplace.Domain.Auth.ValueObjects;
using Marketplace.Domain.Users.ValueObjects;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.Ports
{
    public interface ITokenPort
    {
        AuthToken GenerateAccessToken(IdentityUserId userId, string email, IList<string> roles);
        RefreshToken GenerateRefreshToken();
        TokenValidationResult? ValidateToken(string token);
        string GenerateEmailConfirmationToken(IdentityUserId userId, string email);
        string GeneratePasswordResetToken(IdentityUserId userId, string email);
    }

    public record TokenValidationResult(
        IdentityUserId UserId,
        string Email,
        IList<string> Roles,
        DateTime ExpiresAt
    );
}
