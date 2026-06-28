using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Shipping.Commands.HandleNovaPoshtaWebhook;
using Marketplace.Application.Shipping.Services;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Shipping;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Shipping")]
[Trait("Layer", "IntegrationContainers")]
public sealed class NovaPoshtaWebhookPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public NovaPoshtaWebhookPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Duplicate_Webhook_Event_Is_Idempotent()
    {
        await _fixture.ApplySeedDataAsync();
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handler = new HandleNovaPoshtaWebhookCommandHandler(BuildFulfillmentService(db));

        const string eventKey = "seed-np-dedup-001";
        const string payload = """{"status":"in_transit","trackingNumber":"NP-SEED-0002-A"}""";
        const string hash = "hash-seed-np-dedup-001";

        var first = await handler.Handle(new HandleNovaPoshtaWebhookCommand(eventKey, hash, payload), CancellationToken.None);
        var second = await handler.Handle(new HandleNovaPoshtaWebhookCommand(eventKey, hash, payload), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var events = await db.ShippingEvents.AsNoTracking()
            .Where(x => x.EventKey == eventKey)
            .ToListAsync();
        Assert.Single(events);
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
