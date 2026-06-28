using System.Text.Json;
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
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Tests.Common.Fakes;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Notifications")]
[Trait("Suite", "Orders")]
public sealed class ApplicationUpdateOrderStatusAppNotificationsTests
{
    [Fact]
    public async Task Shipped_From_Processing_Schedules_UserOrderStatus_For_Customer()
    {
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var order = Order.Reconstitute(
            OrderId.From(0),
            "ORD-TEST-1",
            customerId,
            CompanyId.From(companyId),
            OrderStatus.Processing,
            new Money(100),
            new Money(100),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(0),
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

        var orderRepo = new InMemoryOrderRepository();
        _ = await orderRepo.AddAsync(order, CancellationToken.None);

        var spy = new SpyAppNotificationScheduler();
        var handler = new UpdateOrderStatusCommandHandler(
            orderRepo,
            new AllowAllOrderAccess(),
            new NoopShipmentFulfillmentService(),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopOrderStatusHistoryWriter(),
            spy);

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(
                orderRepo.LastId,
                Guid.NewGuid(),
                IsActorAdmin: true,
                OrderStatus.Shipped,
                TrackingNumber: "TN-1",
                IdempotencyKey: "idem-ship-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(spy.Requests);
        var req = spy.Requests[0];
        Assert.Equal(AppNotificationTemplateKeys.UserOrderStatus, req.TemplateKey);
        Assert.Equal(AppNotificationAudienceKind.User, req.Audience);
        Assert.Equal(customerId, req.TargetUserId);
        Assert.True(req.Channels.HasFlag(AppNotificationChannelKind.Push));
        Assert.True(req.Channels.HasFlag(AppNotificationChannelKind.InApp));
        Assert.True(req.Channels.HasFlag(AppNotificationChannelKind.Email));
        Assert.True(req.Channels.HasFlag(AppNotificationChannelKind.Telegram));
        using var doc = JsonDocument.Parse(req.PayloadJson);
        Assert.Equal("Shipped", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Delivered_From_Shipped_Schedules_UserOrderStatus_For_Customer()
    {
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var order = Order.Reconstitute(
            OrderId.From(0),
            "ORD-TEST-2",
            customerId,
            CompanyId.From(companyId),
            OrderStatus.Shipped,
            new Money(50),
            new Money(50),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(0),
            CheckoutPaymentMethod.Card,
            null,
            "TN-OLD",
            now,
            null,
            null,
            null,
            now,
            now,
            false,
            null);

        var orderRepo = new InMemoryOrderRepository();
        _ = await orderRepo.AddAsync(order, CancellationToken.None);

        var spy = new SpyAppNotificationScheduler();
        var handler = new UpdateOrderStatusCommandHandler(
            orderRepo,
            new AllowAllOrderAccess(),
            new NoopShipmentFulfillmentService(),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopOrderStatusHistoryWriter(),
            spy);

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(
                orderRepo.LastId,
                Guid.NewGuid(),
                IsActorAdmin: true,
                OrderStatus.Delivered,
                TrackingNumber: null,
                IdempotencyKey: "idem-del-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(spy.Requests);
        Assert.Equal(AppNotificationTemplateKeys.UserOrderStatus, spy.Requests[0].TemplateKey);
        Assert.Equal(customerId, spy.Requests[0].TargetUserId);
        using var doc = JsonDocument.Parse(spy.Requests[0].PayloadJson);
        Assert.Equal("Delivered", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Processing_From_Paid_Schedules_UserOrderStatus_For_Customer()
    {
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var order = Order.Reconstitute(
            OrderId.From(0),
            "ORD-P-1",
            customerId,
            CompanyId.From(companyId),
            OrderStatus.Paid,
            new Money(40),
            new Money(40),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(0),
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

        var orderRepo = new InMemoryOrderRepository();
        _ = await orderRepo.AddAsync(order, CancellationToken.None);

        var spy = new SpyAppNotificationScheduler();
        var handler = new UpdateOrderStatusCommandHandler(
            orderRepo,
            new AllowAllOrderAccess(),
            new NoopShipmentFulfillmentService(),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopOrderStatusHistoryWriter(),
            spy);

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(
                orderRepo.LastId,
                Guid.NewGuid(),
                IsActorAdmin: true,
                OrderStatus.Processing,
                TrackingNumber: null,
                IdempotencyKey: "idem-proc-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(spy.Requests);
        Assert.Equal(AppNotificationTemplateKeys.UserOrderStatus, spy.Requests[0].TemplateKey);
        Assert.Equal(customerId, spy.Requests[0].TargetUserId);
        using var doc = JsonDocument.Parse(spy.Requests[0].PayloadJson);
        Assert.Equal("Processing", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cancel_From_Pending_Schedules_UserOrderStatus_Cancelled()
    {
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var order = Order.Reconstitute(
            OrderId.From(0),
            "ORD-C-1",
            customerId,
            CompanyId.From(companyId),
            OrderStatus.Pending,
            new Money(15),
            new Money(15),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(0),
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

        var orderRepo = new InMemoryOrderRepository();
        _ = await orderRepo.AddAsync(order, CancellationToken.None);

        var spy = new SpyAppNotificationScheduler();
        var handler = new CancelOrderCommandHandler(
            orderRepo,
            new AllowAllOrderAccess(),
            new OrderCancellationPolicy(Options.Create(new OrderCancellationOptions())),
            OrderTestDoubles.CreateCoordinator(new NoopOrderCacheInvalidationService(), new NoopOutboxWriter()),
            new NoopOrderStatusHistoryWriter(),
            spy,
            new NoopCheckoutInventoryService());

        var result = await handler.Handle(
            new CancelOrderCommand(orderRepo.LastId, Guid.NewGuid(), IsActorAdmin: true, OrderCancellationReasonCode.CustomerRequest, null, "idem-cancel-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(spy.Requests);
        var req = spy.Requests[0];
        Assert.Equal(AppNotificationTemplateKeys.UserOrderStatus, req.TemplateKey);
        Assert.Equal(customerId, req.TargetUserId);
        using var doc = JsonDocument.Parse(req.PayloadJson);
        Assert.Equal("Cancelled", doc.RootElement.GetProperty("status").GetString());
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

    private sealed class AllowAllOrderAccess : IOrderAccessService
    {
        public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<OrderCancellationActor> ResolveCancellationActorAsync(Order order, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default) =>
            Task.FromResult(isActorAdmin ? OrderCancellationActor.Admin : OrderCancellationActor.CompanyMember);
    }

    private sealed class NoopOrderCacheInvalidationService : IOrderCacheInvalidationService
    {
        public Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default) => Task.FromResult(1L);
        public Task TrackDetailKeyAsync(long orderId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task TrackListKeyAsync(string scope, Guid? actorUserId, Guid? companyId, string cacheKey, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopOutboxWriter : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
    }

    private sealed class NoopOrderStatusHistoryWriter : IOrderStatusHistoryWriter
    {
        public Task RecordCreatedAsync(Order order, Guid actorUserId, string source, string? correlationId = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task WriteIfChangedAsync(Order order, OrderStatus oldStatus, Guid actorUserId, string source, string? comment = null, string? correlationId = null, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly Dictionary<long, Order> _items = new();
        private long _nextId = 1;

        public long LastId { get; private set; }

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) =>
            Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Order>>(_items.Values.Where(x => x.CustomerId == customerId).ToList());

        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default)
        {
            IEnumerable<Order> q = _items.Values;
            if (filter.CustomerId.HasValue)
                q = q.Where(x => x.CustomerId == filter.CustomerId.Value);
            if (filter.CompanyId.HasValue)
                q = q.Where(x => x.CompanyId.Value == filter.CompanyId.Value);
            var list = q.ToList();
            return Task.FromResult(((IReadOnlyList<Order>)list, (long)list.Count));
        }

        public Task<Order> AddAsync(Order order, CancellationToken ct = default)
        {
            var id = order.Id.Value <= 0 ? _nextId++ : order.Id.Value;
            LastId = id;
            var saved = Order.Reconstitute(
                OrderId.From(id),
                order.OrderNumber,
                order.CustomerId,
                order.CompanyId,
                order.Status,
                order.TotalPrice,
                order.Subtotal,
                order.ShippingCost,
                order.DiscountAmount,
                order.TaxAmount,
                order.ShippingMethodId,
                order.PaymentMethod,
                order.Notes,
                order.TrackingNumber,
                order.ShippedAt,
                order.DeliveredAt,
                order.CancelledAt,
                order.RefundedAt,
                order.CreatedAt,
                order.UpdatedAt,
                order.IsDeleted,
                order.DeletedAt);
            _items[id] = saved;
            return Task.FromResult(saved);
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            _items[order.Id.Value] = order;
            return Task.CompletedTask;
        }
    }
}
