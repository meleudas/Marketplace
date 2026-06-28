using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Policies;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.Options;
using Marketplace.Application.Orders.Policies;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        var cache = new VersionTrackingCachePort();
        var notificationSpy = new SpyAppNotificationScheduler();
        var order = await orderRepo.AddAsync(BuildOrder(OrderStatus.Paid), CancellationToken.None);

        var handler = new UpdateOrderStatusCommandHandler(
            orderRepo,
            new AllowAccessService(),
            new NoopShipmentFulfillmentService(),
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(cache), outbox),
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
        Assert.True(cache.VersionValues[OrderCacheKeys.MyListVersion(order.CustomerId)] >= 4);
    }

    [Fact]
    public async Task CancelOrder_Persists_Cancelled_Status_History_And_Outbox()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var statusHistoryRepo = new OrderStatusHistoryRepository(db);
        var outbox = new OutboxRepository(db);
        var cache = new VersionTrackingCachePort();
        var order = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending), CancellationToken.None);
        var handler = new CancelOrderCommandHandler(
            orderRepo,
            new AllowAccessService(),
            new OrderCancellationPolicy(Options.Create(new OrderCancellationOptions())),
            OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(cache), outbox),
            new OrderStatusHistoryWriter(statusHistoryRepo),
            new SpyAppNotificationScheduler(),
            new NoopCheckoutInventoryService());

        var versionBefore = await new OrderCacheInvalidationService(cache).GetListVersionAsync("my", order.CustomerId, null, CancellationToken.None);
        var result = await handler.Handle(
            new CancelOrderCommand(order.Id.Value, order.CustomerId, false, OrderCancellationReasonCode.ChangedMind, "test", "idem-order-cancel-1"),
            CancellationToken.None);
        var versionAfter = await new OrderCacheInvalidationService(cache).GetListVersionAsync("my", order.CustomerId, null, CancellationToken.None);

        var saved = await orderRepo.GetByIdAsync(order.Id, CancellationToken.None);
        var history = await statusHistoryRepo.ListByOrderIdAsync(order.Id, CancellationToken.None);
        var outboxRows = await db.OutboxMessages.AsNoTracking().Where(x => x.EventType == "OrderCancelled").ToListAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal(OrderStatus.Cancelled, saved!.Status);
        Assert.Single(history);
        Assert.Single(outboxRows);
        Assert.True(versionAfter > versionBefore);
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

        var listA = await orderRepo.ListAsync(new OrderListFilter(actorA, null, null, null, null, null, null, null, 1, 20), CancellationToken.None);
        var listB = await orderRepo.ListAsync(new OrderListFilter(actorB, null, null, null, null, null, null, null, 1, 20), CancellationToken.None);

        Assert.Single(listA.Items);
        Assert.Single(listB.Items);
        Assert.NotEqual(listA.Items[0].CustomerId, listB.Items[0].CustomerId);
    }

    [Fact]
    public async Task ListAsync_Filters_By_CompanyId_For_Admin_Scope()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        _ = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending, companyId: companyA), CancellationToken.None);
        _ = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending, companyId: companyB), CancellationToken.None);

        var list = await orderRepo.ListAsync(
            new OrderListFilter(null, companyA, null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Single(list.Items);
        Assert.Equal(companyA, list.Items[0].CompanyId.Value);
    }

    [Fact]
    public async Task ListAsync_Filters_By_CompanyMemberUserId_Via_Status_History()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var companyId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var otherManagerId = Guid.NewGuid();
        var orderWithManager = await orderRepo.AddAsync(BuildOrder(OrderStatus.Processing, companyId: companyId), CancellationToken.None);
        var orderWithoutManager = await orderRepo.AddAsync(BuildOrder(OrderStatus.Paid, companyId: companyId), CancellationToken.None);

        await SeedStatusHistoryAsync(db, orderWithManager.Id.Value, managerId, OrderStatus.Pending, OrderStatus.Processing);
        await SeedStatusHistoryAsync(db, orderWithoutManager.Id.Value, otherManagerId, OrderStatus.Pending, OrderStatus.Paid);

        var list = await orderRepo.ListAsync(
            new OrderListFilter(null, companyId, managerId, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Single(list.Items);
        Assert.Equal(orderWithManager.Id.Value, list.Items[0].Id.Value);
    }

    [Fact]
    public async Task ListAsync_Filters_By_Statuses_And_Search()
    {
        await using var db = await CreateSqliteContextAsync();
        var orderRepo = new OrderRepository(db);
        var companyId = Guid.NewGuid();
        var uniqueNumber = $"ORD-SEARCH-{Guid.NewGuid():N}"[..20];
        _ = await orderRepo.AddAsync(BuildOrder(OrderStatus.Pending, companyId: companyId, orderNumber: uniqueNumber), CancellationToken.None);
        _ = await orderRepo.AddAsync(BuildOrder(OrderStatus.Delivered, companyId: companyId), CancellationToken.None);

        var byStatus = await orderRepo.ListAsync(
            new OrderListFilter(null, companyId, null, [OrderStatus.Pending], null, null, null, null, 1, 20),
            CancellationToken.None);
        var bySearch = await orderRepo.ListAsync(
            new OrderListFilter(null, companyId, null, null, null, null, uniqueNumber, null, 1, 20),
            CancellationToken.None);

        Assert.Single(byStatus.Items);
        Assert.Equal(OrderStatus.Pending, byStatus.Items[0].Status);
        Assert.Single(bySearch.Items);
        Assert.Equal(uniqueNumber, bySearch.Items[0].OrderNumber);
    }

    private static async Task SeedStatusHistoryAsync(
        ApplicationDbContext db,
        long orderId,
        Guid changedByUserId,
        OrderStatus oldStatus,
        OrderStatus newStatus)
    {
        var now = DateTime.UtcNow;
        db.OrderStatusHistory.Add(new Marketplace.Infrastructure.Persistence.Entities.OrderStatusHistoryRecord
        {
            OrderId = orderId,
            OldStatus = (short)oldStatus,
            NewStatus = (short)newStatus,
            ChangedByUserId = changedByUserId,
            Source = "manual",
            ChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
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

    private static Order BuildOrder(OrderStatus status, Guid? customerId = null, Guid? companyId = null, string? orderNumber = null)
    {
        var now = DateTime.UtcNow;
        return Order.Reconstitute(
            OrderId.From(0),
            orderNumber ?? $"ORD-SQL-{Guid.NewGuid():N}"[..16],
            customerId ?? Guid.NewGuid(),
            CompanyId.From(companyId ?? Guid.NewGuid()),
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

        public Task<OrderCancellationActor> ResolveCancellationActorAsync(Order order, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
        {
            if (isActorAdmin)
                return Task.FromResult(OrderCancellationActor.Admin);
            if (order.CustomerId == actorUserId)
                return Task.FromResult(OrderCancellationActor.Buyer);
            return Task.FromResult(OrderCancellationActor.CompanyMember);
        }
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

    private sealed class NoopShipmentFulfillmentService : IShipmentFulfillmentService
    {
        public Task<Result<Shipment>> CreateShipmentAsync(
            Order order,
            WarehouseId? warehouseId,
            IReadOnlyList<CreateShipmentLineRequest> lines,
            string? trackingNumber,
            Guid actorUserId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<Shipment>.Failure("not used"));

        public Task<Result> ApplyCarrierEventAsync(
            ShippingCarrierCode carrier,
            string eventKey,
            string payloadHash,
            string rawPayload,
            CancellationToken ct = default) =>
            Task.FromResult(Result.Success());

        public Task<ShipmentFulfillmentSummary> BuildSummaryAsync(OrderId orderId, CancellationToken ct = default) =>
            Task.FromResult(new ShipmentFulfillmentSummary(0, 0, 0, false, false));

        public Task<FulfillmentReadinessDto> BuildReadinessDtoAsync(OrderId orderId, CancellationToken ct = default) =>
            Task.FromResult(new FulfillmentReadinessDto(1, 1, 0, true, false, [], [], []));
    }
}
