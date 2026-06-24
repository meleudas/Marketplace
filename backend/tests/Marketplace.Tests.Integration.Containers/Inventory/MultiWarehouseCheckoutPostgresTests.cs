using Marketplace.Infrastructure.Persistence;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Inventory;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Inventory")]
[Trait("Layer", "IntegrationContainers")]
public sealed class MultiWarehouseCheckoutPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public MultiWarehouseCheckoutPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Seed_Order4_Has_Split_Allocations_Across_Two_Warehouses()
    {
        await _fixture.ApplySeedDataAsync();
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var allocations = await db.OrderFulfillmentAllocations.AsNoTracking()
            .Where(x => x.OrderId == SeedTestConstants.OrderPaidSplitId && !x.IsDeleted)
            .OrderBy(x => x.WarehouseId)
            .ToListAsync();

        Assert.Equal(2, allocations.Count);
        Assert.Contains(allocations, x => x.WarehouseId == SeedTestConstants.WarehouseKyivId && x.Quantity == 1);
        Assert.Contains(allocations, x => x.WarehouseId == SeedTestConstants.WarehouseLvivId && x.Quantity == 1);
    }
}
