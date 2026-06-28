using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Shipping.Commands.CreateShipment;
using Marketplace.Application.Shipping.Policies;
using Marketplace.Application.Shipping.Services;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Shipping;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Shipping")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ShipmentFulfillmentPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ShipmentFulfillmentPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_Shipment_For_Order4_Warehouse1_Marks_Allocation_Shipped()
    {
        await _fixture.ApplySeedDataAsync();
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var orderRepo = new OrderRepository(db);
        var fulfillment = BuildFulfillmentService(db);
        var handler = new CreateShipmentCommandHandler(
            orderRepo,
            new ShipmentAccessPolicy(new OrderAccessService(new CompanyMemberRepository(db))),
            fulfillment,
            new ShipmentItemRepository(db));

        var result = await handler.Handle(
            new CreateShipmentCommand(
                SeedTestConstants.OrderPaidSplitId,
                SeedTestConstants.TechStoreCompanyId,
                SeedTestConstants.SellerUserId,
                false,
                SeedTestConstants.WarehouseKyivId,
                [new CreateShipmentLineDto(5, 1)],
                "NP-SEED-0004-A"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var allocation = await db.OrderFulfillmentAllocations.AsNoTracking()
            .SingleAsync(x => x.OrderId == SeedTestConstants.OrderPaidSplitId
                && x.WarehouseId == SeedTestConstants.WarehouseKyivId
                && !x.IsDeleted);
        Assert.Equal((short)3, allocation.Status);
    }

    private static ShipmentFulfillmentService BuildFulfillmentService(ApplicationDbContext db)
    {
        var outbox = new OutboxRepository(db);
        return new ShipmentFulfillmentService(
            new OrderRepository(db),
            new OrderItemRepository(db),
            new ShipmentRepository(db),
            new ShipmentItemRepository(db),
            new ShippingEventRepository(db),
            new OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), outbox),
            new OrderFulfillmentAllocationRepository(db),
            new FulfillmentInventoryService(
                new OrderFulfillmentAllocationRepository(db),
                new WarehouseStockRepository(db),
                new StockMovementRepository(db)),
            new WarehouseRepository(db));
    }
}
