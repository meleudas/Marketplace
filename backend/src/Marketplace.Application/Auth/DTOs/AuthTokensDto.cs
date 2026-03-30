using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.DTOs
{
    public record AuthTokensDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        DateTime RefreshTokenExpiresAt
    );
}
