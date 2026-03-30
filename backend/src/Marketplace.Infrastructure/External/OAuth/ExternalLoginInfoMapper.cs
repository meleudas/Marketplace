using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.Infrastructure.External.OAuth;

/// <summary>Мапінг <see cref="ExternalLoginInfo"/> до полів для створення/прив’язки користувача Identity.</summary>
public static class ExternalLoginInfoMapper
{
    public static string? GetEmail(ExternalLoginInfo info) =>
        info.Principal.FindFirst(ClaimTypes.Email)?.Value;
}
