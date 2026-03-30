using Marketplace.Domain.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Auth.ValueObjects
{
    public record AuthToken : ValueObject
    {
        public string Value { get; init; }
        public DateTime ExpiresAt { get; init; }
        public string TokenType { get; init; } 

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        private AuthToken(string value, DateTime expiresAt, string tokenType = "Bearer")
        {
            Value = value;
            ExpiresAt = expiresAt;
            TokenType = tokenType;
        }

        public static AuthToken Create(string value, TimeSpan ttl)
        {
            return new AuthToken(value, DateTime.UtcNow.Add(ttl));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
            yield return ExpiresAt;
            yield return TokenType;
        }
    }
}
