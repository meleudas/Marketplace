using Marketplace.Domain.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Auth.ValueObjects
{
    public record AuthTokens : ValueObject
    {
        public AuthToken AccessToken { get; init; }
        public RefreshToken RefreshToken { get; init; }

        private AuthTokens(AuthToken accessToken, RefreshToken refreshToken)
        {
            AccessToken = accessToken ?? throw new Common.Exceptions.DomainException("Access token cannot be null");
            RefreshToken = refreshToken ?? throw new Common.Exceptions.DomainException("Refresh token cannot be null");
        }

        public static AuthTokens Create(AuthToken accessToken, RefreshToken refreshToken)
        {
            return new AuthTokens(accessToken, refreshToken);
        }

        public static AuthTokens Create(string accessTokenValue, TimeSpan accessTtl, string refreshTokenValue, int refreshTtlDays = 7)
        {
            var accessToken = AuthToken.Create(accessTokenValue, accessTtl);
            var refreshToken = RefreshToken.Create(refreshTokenValue, refreshTtlDays);
            return new AuthTokens(accessToken, refreshToken);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AccessToken;
            yield return RefreshToken;
        }
    }
}
