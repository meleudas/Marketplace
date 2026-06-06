using System.Text.Json;
using Marketplace.Application.Auth.DTOs;

namespace Marketplace.Tests;

public sealed class ContractAuthDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "IdentityAccess")]
    public void Contract_AuthTokensDto_Snapshot_Matches()
    {
        var dto = new AuthTokensDto(
            "access-token",
            "refresh-token",
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddDays(30));

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"AccessToken\":\"access-token\",\"RefreshToken\":\"refresh-token\",\"AccessTokenExpiresAt\":\"1970-01-01T00:00:00Z\",\"RefreshTokenExpiresAt\":\"1970-01-31T00:00:00Z\"}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "IdentityAccess")]
    public void Contract_TwoFactorStatusDto_Snapshot_Matches()
    {
        var dto = new TwoFactorStatusDto(true, false, true);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"TwoFactorEnabled\":true,\"TelegramTwoFactorEnabled\":false,\"TelegramLinked\":true}";
        Assert.Equal(expected, json);
    }
}
