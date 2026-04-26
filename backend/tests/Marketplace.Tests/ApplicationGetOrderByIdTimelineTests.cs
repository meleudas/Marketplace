using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Queries.GetOrderById;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

public sealed class ApplicationGetOrderByIdTimelineTests
{
    [Fact]
    public async Task Returns_Status_History_In_Details()
    {
        var actorId = Guid.NewGuid();
        var order = Order.Reconstitute(
            OrderId.From(10),
            "ORD-10",
            actorId,
            CompanyId.From(Guid.NewGuid()),
            OrderStatus.Processing,
            new Money(100),
            new Money(100),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            ShippingMethodId.From(1),
            CheckoutPaymentMethod.Card,
            null, null, null, null, null, null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

        var handler = new GetOrderByIdQueryHandler(
            new StubOrderRepository(order),
            new StubOrderItemRepo(),
            new StubAddressRepo(),
            new StubStatusHistoryRepo(order.Id),
            new StubPaymentRepo(),
            new StubRefundRepo(),
            new AllowAllOrderAccessService(),
            new NullCache(),
            Options.Create(new CacheTtlOptions()));

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id.Value, actorId, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value!.StatusHistory);
        Assert.Equal("Paid", result.Value.StatusHistory.Last().OldStatus);
    }

    private sealed class AllowAllOrderAccessService : IOrderAccessService
    {
        public Task<bool> CanReadCompanyScopeAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> HasAccessAsync(Order order, Guid actorUserId, bool isActorAdmin, OrderPermission permission, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class NullCache : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOrderRepository : IOrderRepository
    {
        private readonly Order _order;
        public StubOrderRepository(Order order) => _order = order;
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult<Order?>(_order);
        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Order>>([]);
        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default) => Task.FromResult(((IReadOnlyList<Order>)[], 0L));
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) => Task.FromResult(order);
        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOrderItemRepo : IOrderItemRepository
    {
        public Task<IReadOnlyList<OrderItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OrderItem>>([]);
        public Task AddRangeAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubAddressRepo : IOrderAddressSnapshotRepository
    {
        public Task<IReadOnlyList<OrderAddressSnapshot>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OrderAddressSnapshot>>([]);
        public Task AddRangeAsync(IReadOnlyList<OrderAddressSnapshot> addresses, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubStatusHistoryRepo : IOrderStatusHistoryRepository
    {
        private readonly OrderId _orderId;
        public StubStatusHistoryRepo(OrderId orderId) => _orderId = orderId;
        public Task AddAsync(OrderStatusHistory history, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OrderStatusHistory>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var items = new List<OrderStatusHistory>
            {
                OrderStatusHistory.Reconstitute(OrderStatusHistoryId.From(1), _orderId, OrderStatus.Pending, OrderStatus.Paid, null, Guid.Empty, "webhook", "a", now.AddMinutes(-2), now.AddMinutes(-2), now.AddMinutes(-2), false, null),
                OrderStatusHistory.Reconstitute(OrderStatusHistoryId.From(2), _orderId, OrderStatus.Paid, OrderStatus.Processing, null, Guid.Empty, "manual", "b", now.AddMinutes(-1), now.AddMinutes(-1), now.AddMinutes(-1), false, null)
            };
            return Task.FromResult<IReadOnlyList<OrderStatusHistory>>(items);
        }
    }

    private sealed class StubPaymentRepo : IPaymentRepository
    {
        public Task<Marketplace.Domain.Payments.Entities.Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default) => Task.FromResult<Marketplace.Domain.Payments.Entities.Payment?>(null);
        public Task<Marketplace.Domain.Payments.Entities.Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<Marketplace.Domain.Payments.Entities.Payment?>(null);
        public Task<Marketplace.Domain.Payments.Entities.Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default) => Task.FromResult<Marketplace.Domain.Payments.Entities.Payment?>(null);
        public Task<IReadOnlyList<Marketplace.Domain.Payments.Entities.Payment>> ListByStatusAsync(Marketplace.Domain.Payments.Enums.PaymentTransactionStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Marketplace.Domain.Payments.Entities.Payment>>([]);
        public Task<Marketplace.Domain.Payments.Entities.Payment> AddAsync(Marketplace.Domain.Payments.Entities.Payment payment, CancellationToken ct = default) => Task.FromResult(payment);
        public Task UpdateAsync(Marketplace.Domain.Payments.Entities.Payment payment, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubRefundRepo : IRefundRepository
    {
        public Task<Marketplace.Domain.Payments.Entities.Refund?> GetByIdAsync(RefundId id, CancellationToken ct = default) => Task.FromResult<Marketplace.Domain.Payments.Entities.Refund?>(null);
        public Task<IReadOnlyList<Marketplace.Domain.Payments.Entities.Refund>> ListByStatusAsync(Marketplace.Domain.Payments.Enums.RefundStatus status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Marketplace.Domain.Payments.Entities.Refund>>([]);
        public Task<IReadOnlyList<Marketplace.Domain.Payments.Entities.Refund>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Marketplace.Domain.Payments.Entities.Refund>>([]);
        public Task<Marketplace.Domain.Payments.Entities.Refund> AddAsync(Marketplace.Domain.Payments.Entities.Refund refund, CancellationToken ct = default) => Task.FromResult(refund);
        public Task UpdateAsync(Marketplace.Domain.Payments.Entities.Refund refund, CancellationToken ct = default) => Task.CompletedTask;
    }
}
