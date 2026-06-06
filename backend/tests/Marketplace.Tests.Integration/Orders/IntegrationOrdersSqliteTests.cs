using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public sealed class IntegrationOrdersSqliteTests
{
    [Fact]
    public async Task UpdateStatus_Lifecycle_Persists_Order_History_And_Outbox()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var statusHistoryRepo = new OrderStatusHistoryRepository(db);
        var outbox = new OutboxRepository(db);
        var cache = new SpyCachePort();
        var notificationSpy = new SpyAppNotificationScheduler();

        var order = await orderRepo.AddAsync(BuildOrder(OrderStatus.Paid), CancellationToken.None);
        var handler = new UpdateOrderStatusCommandHandler(
            orderRepo,
            new AllowAccessService(),
            new OrderCacheInvalidationService(cache),
            outbox,
            new OrderStatusHistoryWriter(statusHistoryRepo),
            notificationSpy);

        var processing = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, Guid.NewGuid(), false, OrderStatus.Processing, null, "idem-order-lifecycle-1"),
            CancellationToken.None);
        var shipped = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, Guid.NewGuid(), false, OrderStatus.Shipped, "TRK-100", "idem-order-lifecycle-2"),
            CancellationToken.None);
        var delivered = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, Guid.NewGuid(), false, OrderStatus.Delivered, null, "idem-order-lifecycle-3"),
            CancellationToken.None);

        var saved = await orderRepo.GetByIdAsync(order.Id, CancellationToken.None);
        var history = await statusHistoryRepo.ListByOrderIdAsync(order.Id, CancellationToken.None);
        var outboxRows = await db.OutboxMessages.AsNoTracking().Where(x => x.AggregateType == "Order").ToListAsync();

        Assert.True(processing.IsSuccess);
        Assert.True(shipped.IsSuccess);
        Assert.True(delivered.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal(OrderStatus.Delivered, saved!.Status);
        Assert.Equal(3, history.Count);
        Assert.Equal(3, outboxRows.Count);
        Assert.Equal(3, notificationSpy.Requests.Count);
        Assert.Contains(cache.RemovedKeys, x => x == OrderCacheKeys.Detail(order.Id.Value));
    }

    [Fact]
    public async Task CancelOrder_Persists_Cancelled_Status_History_And_Outbox()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var statusHistoryRepo = new OrderStatusHistoryRepository(db);
        var outbox = new OutboxRepository(db);
        var cache = new SpyCachePort();
        var handler = new CancelOrderCommandHandler(
            orderRepo,
            new AllowAccessService(),
            new OrderCacheInvalidationService(cache),
            outbox,
            new OrderStatusHistoryWriter(statusHistoryRepo),
            new SpyAppNotificationScheduler());

        var order = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending), CancellationToken.None);
        var result = await handler.Handle(
            new CancelOrderCommand(order.Id.Value, Guid.NewGuid(), false, "idem-order-cancel-1"),
            CancellationToken.None);

        var saved = await orderRepo.GetByIdAsync(order.Id, CancellationToken.None);
        var history = await statusHistoryRepo.ListByOrderIdAsync(order.Id, CancellationToken.None);
        var outboxRows = await db.OutboxMessages.AsNoTracking().Where(x => x.EventType == "OrderCancelled").ToListAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal(OrderStatus.Cancelled, saved!.Status);
        Assert.Single(history);
        Assert.Single(outboxRows);
    }

    [Fact]
    public async Task ListOrdersQuery_Is_Isolated_By_Customer()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var actorA = Guid.NewGuid();
        var actorB = Guid.NewGuid();
        _ = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending, actorA), CancellationToken.None);
        _ = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending, actorB), CancellationToken.None);

        var listA = await orderRepo.ListAsync(new OrderListFilter(actorA, null, null, null, null, null, null, 1, 20), CancellationToken.None);
        var listB = await orderRepo.ListAsync(new OrderListFilter(actorB, null, null, null, null, null, null, 1, 20), CancellationToken.None);

        Assert.Single(listA.Items);
        Assert.Single(listB.Items);
        Assert.NotEqual(listA.Items[0].CustomerId, listB.Items[0].CustomerId);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Order BuildOrder(OrderStatus status, Guid? customerId = null)
    {
        var now = DateTime.UtcNow;
        return Order.Reconstitute(
            OrderId.From(0),
            $"ORD-SQL-{Guid.NewGuid():N}"[..16],
            customerId ?? Guid.NewGuid(),
            CompanyId.From(Guid.NewGuid()),
            status,
            new Money(100),
            new Money(100),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(1),
            CheckoutPaymentMethod.Card,
            null,
            null,
            null,
            null,
            null,
            null,
            now,
            now,
            false,
            null);
    }

    private sealed class AllowAccessService : IOrderAccessService
    {
        public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private sealed class SpyAppNotificationScheduler : IAppNotificationScheduler
    {
        public List<AppNotificationRequest> Requests { get; } = [];

        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class SpyCachePort : IAppCachePort
    {
        private readonly Dictionary<string, object> _items = new();
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_items.TryGetValue(key, out var value) ? value as T : null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            _items[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            _items.Remove(key);
            return Task.CompletedTask;
        }
    }
}
