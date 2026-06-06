using System.Text.Json;
using Marketplace.Application.Favorites.DTOs;

namespace Marketplace.Tests;

public sealed class ContractFavoriteDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Favorites")]
    public void Contract_FavoriteItemDto_Snapshot_Matches()
    {
        var dto = new FavoriteItemDto(
            15,
            25,
            DateTime.UnixEpoch,
            199.99m,
            true);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Id\":15,\"ProductId\":25,\"AddedAt\":\"1970-01-01T00:00:00Z\",\"PriceAtAdd\":199.99,\"IsAvailable\":true}";
        Assert.Equal(expected, json);
    }
}
