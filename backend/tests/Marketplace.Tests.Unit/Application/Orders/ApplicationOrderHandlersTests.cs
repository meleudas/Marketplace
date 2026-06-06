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

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public sealed class ApplicationOrderHandlersTests
{
    [Fact]
    public async Task UpdateOrderStatus_Returns_Forbidden_When_Access_Denied()
    {
        var order = BuildOrder(status: OrderStatus.Paid);
        var repo = new InMemoryOrderRepository(order);
        var handler = new UpdateOrderStatusCommandHandler(
            repo,
            new StubOrderAccessService(false),
            new NoopOrderCacheInvalidationService(),
            new SpyOutboxWriter(),
            new SpyOrderStatusHistoryWriter(),
            new SpyAppNotificationScheduler());

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, Guid.NewGuid(), false, OrderStatus.Processing, null, "idem-orders-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task UpdateOrderStatus_Returns_Failure_For_Invalid_Transition()
    {
        var order = BuildOrder(status: OrderStatus.Pending);
        var repo = new InMemoryOrderRepository(order);
        var handler = new UpdateOrderStatusCommandHandler(
            repo,
            new StubOrderAccessService(true),
            new NoopOrderCacheInvalidationService(),
            new SpyOutboxWriter(),
            new SpyOrderStatusHistoryWriter(),
            new SpyAppNotificationScheduler());

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, Guid.NewGuid(), false, OrderStatus.Delivered, null, "idem-orders-2"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid status transition", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateOrderStatus_Succeeds_And_Writes_SideEffects()
    {
        var order = BuildOrder(status: OrderStatus.Paid);
        var repo = new InMemoryOrderRepository(order);
        var outbox = new SpyOutboxWriter();
        var history = new SpyOrderStatusHistoryWriter();
        var notifications = new SpyAppNotificationScheduler();
        var handler = new UpdateOrderStatusCommandHandler(
            repo,
            new StubOrderAccessService(true),
            new NoopOrderCacheInvalidationService(),
            outbox,
            history,
            notifications);

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id.Value, Guid.NewGuid(), false, OrderStatus.Processing, null, "idem-orders-3"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Processing, repo.Current!.Status);
        Assert.Contains(outbox.Events, x => x.EventType == "OrderStatusChanged");
        Assert.Single(history.Entries);
        Assert.Single(notifications.Requests);
        Assert.Equal(AppNotificationTemplateKeys.UserOrderStatus, notifications.Requests[0].TemplateKey);
    }

    [Fact]
    public async Task CancelOrder_Returns_Forbidden_When_Access_Denied()
    {
        var order = BuildOrder(status: OrderStatus.Pending);
        var repo = new InMemoryOrderRepository(order);
        var handler = new CancelOrderCommandHandler(
            repo,
            new StubOrderAccessService(false),
            new NoopOrderCacheInvalidationService(),
            new SpyOutboxWriter(),
            new SpyOrderStatusHistoryWriter(),
            new SpyAppNotificationScheduler());

        var result = await handler.Handle(
            new CancelOrderCommand(order.Id.Value, Guid.NewGuid(), false, "idem-orders-4"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task CancelOrder_Succeeds_And_Writes_SideEffects()
    {
        var order = BuildOrder(status: OrderStatus.Pending);
        var repo = new InMemoryOrderRepository(order);
        var outbox = new SpyOutboxWriter();
        var history = new SpyOrderStatusHistoryWriter();
        var notifications = new SpyAppNotificationScheduler();
        var handler = new CancelOrderCommandHandler(
            repo,
            new StubOrderAccessService(true),
            new NoopOrderCacheInvalidationService(),
            outbox,
            history,
            notifications);

        var result = await handler.Handle(
            new CancelOrderCommand(order.Id.Value, Guid.NewGuid(), false, "idem-orders-5"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, repo.Current!.Status);
        Assert.Contains(outbox.Events, x => x.EventType == "OrderCancelled");
        Assert.Single(history.Entries);
        Assert.Single(notifications.Requests);
    }

    private static Order BuildOrder(OrderStatus status)
    {
        var now = DateTime.UtcNow;
        return Order.Reconstitute(
            OrderId.From(101),
            "ORD-APP-101",
            Guid.NewGuid(),
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

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        public InMemoryOrderRepository(Order order) => Current = order;
        public Order? Current { get; private set; }

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
            => Task.FromResult(Current is not null && Current.Id == id ? Current : null);

        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Order>>(Current is not null && Current.CustomerId == customerId ? [Current] : []);

        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<Order>, long)>((Current is null ? [] : [Current], Current is null ? 0 : 1));

        public Task<Order> AddAsync(Order order, CancellationToken ct = default)
        {
            Current = order;
            return Task.FromResult(order);
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            Current = order;
            return Task.CompletedTask;
        }
    }

    private sealed class StubOrderAccessService : IOrderAccessService
    {
        private readonly bool _allow;
        public StubOrderAccessService(bool allow) => _allow = allow;

        public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default)
            => Task.FromResult(_allow);

        public Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default)
            => Task.FromResult(_allow);
    }

    private sealed class NoopOrderCacheInvalidationService : IOrderCacheInvalidationService
    {
        public Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default) => Task.FromResult(1L);
        public Task TrackDetailKeyAsync(long orderId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task TrackListKeyAsync(string scope, Guid? actorUserId, Guid? companyId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SpyOutboxWriter : IOutboxWriter
    {
        public List<(string AggregateType, string AggregateId, string EventType)> Events { get; } = [];

        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
        {
            Events.Add((aggregateType, aggregateId, eventType));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SpyOrderStatusHistoryWriter : IOrderStatusHistoryWriter
    {
        public List<(OrderStatus OldStatus, OrderStatus NewStatus)> Entries { get; } = [];

        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default)
        {
            if (oldStatus != order.Status)
                Entries.Add((oldStatus, order.Status));
            return Task.CompletedTask;
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
}
