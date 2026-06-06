using Marketplace.Application.Carts.Commands.CheckoutCart;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Coupons.Services;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "CartCheckout")]
[Trait("Suite", "Platform")]
public sealed class IntegrationCartCheckoutSqliteTests
{
    [Fact]
    public async Task Checkout_With_Real_Db_Persists_Order_Payment_Outbox_And_Clears_Cart()
    {
        await using var db = await CreateSqliteContextAsync();
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
            new OrderCacheInvalidationService(new NoopCachePort()),
            new OutboxRepository(db),
            new NoopOrderStatusHistoryWriter(),
            new WarehouseStockRepository(db),
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
                "integration",
                "idem-sqlite-success"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(await db.Orders.AsNoTracking().ToListAsync());
        Assert.Single(await db.Payments.AsNoTracking().ToListAsync());
        Assert.Single(await db.OutboxMessages.AsNoTracking().ToListAsync());
        Assert.Empty(await db.CartItems.AsNoTracking().Where(x => x.CartId == fixture.CartId && !x.IsDeleted).ToListAsync());
    }

    [Fact]
    public async Task Checkout_With_Real_Db_Rolls_Back_When_Outbox_Fails()
    {
        await using var db = await CreateSqliteContextAsync();
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
            new OrderCacheInvalidationService(new NoopCachePort()),
            new ThrowingOutboxWriter(),
            new NoopOrderStatusHistoryWriter(),
            new WarehouseStockRepository(db),
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
                "integration",
                "idem-sqlite-rollback"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(await db.Orders.AsNoTracking().ToListAsync());
        Assert.Empty(await db.Payments.AsNoTracking().ToListAsync());
        Assert.Empty(await db.OutboxMessages.AsNoTracking().ToListAsync());
        Assert.Single(await db.CartItems.AsNoTracking().Where(x => x.CartId == fixture.CartId && !x.IsDeleted).ToListAsync());
    }

    [Fact]
    public async Task HttpIdempotencyStore_Replays_Stored_Response_For_Same_Key()
    {
        await using var db = await CreateSqliteContextAsync();
        var store = new HttpIdempotencyStore(db);
        var scope = "checkout:test-user";
        var key = "idem-db-1";
        var hash = "hash-1";

        var started = await store.TryBeginAsync(scope, key, hash, TimeSpan.FromHours(1), CancellationToken.None);
        Assert.Equal(HttpIdempotencyBeginState.Started, started.State);

        await store.CompleteAsync(scope, key, hash, 200, """{"ok":true}""", CancellationToken.None);
        var replay = await store.TryBeginAsync(scope, key, hash, TimeSpan.FromHours(1), CancellationToken.None);

        Assert.Equal(HttpIdempotencyBeginState.Completed, replay.State);
        Assert.NotNull(replay.StoredResponse);
        Assert.Equal(200, replay.StoredResponse!.StatusCode);
        Assert.Equal("""{"ok":true}""", replay.StoredResponse.ResponseBodyJson);
    }

    private static async Task<(Guid UserId, long CartId)> SeedCheckoutFixtureAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        const long productId = 1001;

        var productRepository = new ProductRepository(db);
        await productRepository.AddAsync(
            Marketplace.Domain.Catalog.Entities.Product.Reconstitute(
                ProductId.From(productId),
                CompanyId.From(companyId),
                "Test Product",
                "test-product-sqlite",
                "Description",
                new Money(150),
                null,
                10,
                0,
                CategoryId.From(1),
                Marketplace.Domain.Catalog.Enums.ProductStatus.Active,
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
            Cart.Reconstitute(CartId.From(0), userId, CartStatus.Active, now, now, now, false, null),
            CancellationToken.None);

        var cartItemRepository = new CartItemRepository(db);
        await cartItemRepository.AddAsync(
            CartItem.Reconstitute(CartItemId.From(0), cart.Id, ProductId.From(productId), 1, new Money(150), Money.Zero, now, now, false, null),
            CancellationToken.None);

        var stockRepository = new WarehouseStockRepository(db);
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

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class NoopCachePort : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopOrderStatusHistoryWriter : IOrderStatusHistoryWriter
    {
        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NoopAppNotificationScheduler : IAppNotificationScheduler
    {
        public Task ScheduleAsync(Marketplace.Application.Notifications.AppNotificationRequest request, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopCartStockWatchRepository : ICartStockWatchRepository
    {
        public Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(long productId, TimeSpan minIntervalSinceLastNotify, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeLiqPayPort : ILiqPayPort
    {
        public Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
            => Task.FromResult(new LiqPayCreatePaymentResult(true, request.OrderNumber, "https://liqpay.test", "data", "sig", "{\"status\":\"ok\"}", null));

        public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default) => Task.FromResult(true);
        public Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default) => Task.FromResult(new LiqPayPaymentStatusResult(true, transactionId, "success", "{}", null));
        public Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default) => Task.FromResult(new LiqPayRefundResult(true, request.TransactionId, "ok", "{}", null));
        public Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default) => Task.FromResult(new LiqPayHealthResult(true, "LiqPay", "ok"));
        public LiqPayConfigHealthResult CheckConfig() => new(true, "ok");
    }

    private sealed class ThrowingOutboxWriter : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
            => throw new InvalidOperationException("simulated outbox failure");

        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopCouponCheckoutService : ICouponCheckoutService
    {
        public Task<CheckoutCouponDiscountResult> ResolveDiscountAsync(Guid actorUserId, CartId cartId, CompanyId companyId, decimal subtotal, CancellationToken ct = default)
            => Task.FromResult(new CheckoutCouponDiscountResult(0, null, null));

        public Task ConsumeAsync(Guid actorUserId, OrderId orderId, long couponId, string couponCode, decimal discountAmount, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
