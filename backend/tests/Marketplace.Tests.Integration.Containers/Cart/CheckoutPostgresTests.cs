using Marketplace.Application.Carts.Commands.CheckoutCart;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Marketplace.Tests.Cart;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "CartCheckout")]
[Trait("Suite", "Platform")]
[Trait("Layer", "IntegrationContainers")]
public sealed class CheckoutPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public CheckoutPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Checkout_With_Postgres_Persists_Order_Payment_Outbox_And_Clears_Cart()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fixture = await SeedCheckoutFixtureAsync(db);

        var handler = new CheckoutCartCommandHandler(
            new CartRepository(db),
            new CartItemRepository(db),
            new ProductRepository(db),
            new OrderRepository(db),
            new OrderItemRepository(db),
            new OrderAddressSnapshotRepository(db),
            new PaymentRepository(db),
            new FakeLiqPayPort(),
            new NoopCachePort(),
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), new OutboxRepository(db)),
            new NoopCheckoutInventoryService(),
            new NoopOrderStatusHistoryWriter(),
            new WarehouseStockRepository(db),
            new WarehouseAllocationPlanner(new WarehouseRepository(db), new WarehouseStockRepository(db)),
            new NoopAppNotificationScheduler(),
            new NoopCartStockWatchRepository(),
            new ShippingMethodRepository(db),
            new NoopCouponCheckoutService(),
            new AppTransactionPort(db),
            NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(
            new CheckoutCartCommand(
                fixture.UserId,
                CheckoutPaymentMethod.Card,
                1,
                new CheckoutAddressDto("A", "B", "+380", "Street", "Kyiv", "Kyiv", "01001", "UA"),
                "containers",
                "idem-pg-checkout-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(await db.Orders.AsNoTracking().ToListAsync());
        Assert.Single(await db.Payments.AsNoTracking().ToListAsync());
        Assert.Single(await db.OutboxMessages.AsNoTracking().Where(x => x.AggregateType == "Order").ToListAsync());
        Assert.Empty(await db.CartItems.AsNoTracking().Where(x => x.CartId == fixture.CartId && !x.IsDeleted).ToListAsync());
    }

    private static async Task<(Guid UserId, long CartId)> SeedCheckoutFixtureAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        const long productId = 1001;

        var productRepository = new ProductRepository(db);
        await productRepository.AddAsync(
            Product.Reconstitute(
                ProductId.From(productId),
                CompanyId.From(companyId),
                "Test Product",
                "test-product-pg",
                "Description",
                new Money(150),
                null,
                10,
                0,
                CategoryId.From(1),
                ProductStatus.Active,
                null,
                0,
                0,
                0,
                false,
                now,
                now,
                false,
                null),
            CancellationToken.None);

        var cartRepository = new CartRepository(db);
        var cart = await cartRepository.AddAsync(
            Marketplace.Domain.Cart.Entities.Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null),
            CancellationToken.None);

        var cartItemRepository = new CartItemRepository(db);
        await cartItemRepository.AddAsync(
            CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(productId), 1, new Money(150), Money.Zero, now, now, false, null),
            CancellationToken.None);

        var stockRepository = new WarehouseStockRepository(db);
        var warehouseRepository = new WarehouseRepository(db);
        await warehouseRepository.AddAsync(
            Marketplace.Domain.Inventory.Entities.Warehouse.Create(
                WarehouseId.From(1),
                CompanyId.From(companyId),
                "Main",
                "MAIN",
                Address.Empty,
                "UTC",
                1),
            CancellationToken.None);
        await stockRepository.AddAsync(
            WarehouseStock.Reconstitute(
                WarehouseStockId.From(0),
                CompanyId.From(companyId),
                WarehouseId.From(1),
                ProductId.From(productId),
                10,
                0,
                0,
                1,
                now,
                now,
                false,
                null),
            CancellationToken.None);

        db.ShippingMethods.Add(new Marketplace.Infrastructure.Persistence.Entities.ShippingMethodRecord
        {
            Id = 1,
            Name = "Nova Poshta",
            Code = 0,
            Price = 99,
            FreeShippingThreshold = null,
            EstimatedDaysMin = 1,
            EstimatedDaysMax = 3,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        return (userId, cart.Id.Value);
    }
}
