using Marketplace.Domain.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Auth.ValueObjects
{
    public record RefreshToken : ValueObject
    {
        public string Token { get; init; }
        public DateTime ExpiresAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? RevokedAt { get; init; }
        public string? ReplacedByToken { get; init; }
        public string? ReasonRevoked { get; init; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken(string token, DateTime expiresAt)
        {
            Token = token;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }

        /// <param name="ttlDays">За замовчуванням 30 днів (політика refresh cookie).</param>
        public static RefreshToken Create(string token, int ttlDays = 30)
        {
            return new RefreshToken(token, DateTime.UtcNow.AddDays(ttlDays));
        }

        public RefreshToken Revoke(string reason, string? replacedByToken = null)
        {
            return this with
            {
                RevokedAt = DateTime.UtcNow,
                ReasonRevoked = reason,
                ReplacedByToken = replacedByToken
            };
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Token;
        }
    }
}
