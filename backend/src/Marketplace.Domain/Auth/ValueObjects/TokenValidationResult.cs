using Marketplace.Domain.Users.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Auth.ValueObjects
{
    public record TokenValidationResult(
    IdentityUserId UserId,
    string Email,
    IList<string> Roles,
    DateTime ExpiresAt,
    bool IsValid
    )
    {
        public static TokenValidationResult Valid(IdentityUserId userId, string email, IList<string> roles, DateTime expiresAt)
        {
            return new TokenValidationResult(userId, email, roles, expiresAt, true);
        }

        public static TokenValidationResult Invalid()
        {
            return new TokenValidationResult(IdentityUserId.New(), string.Empty, Array.Empty<string>(), DateTime.MinValue, false);
        }
    }
}
