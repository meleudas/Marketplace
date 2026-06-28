using Marketplace.Application.Reviews.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Reviews")]
public sealed class ApplicationReviewPurchaseVerificationTests
{
    [Fact]
    public async Task VerifiedProductOrderId_Returns_Latest_Eligible_Order_With_Product_Item()
    {
        var userId = Guid.NewGuid();
        var productId = ProductId.From(55);
        var newer = BuildOrder(2, userId, OrderStatus.Delivered, DateTime.UtcNow);
        var older = BuildOrder(1, userId, OrderStatus.Paid, DateTime.UtcNow.AddDays(-1));
        var orderRepo = new StubOrderRepository([older, newer]);
        var itemRepo = new StubOrderItemRepository(new Dictionary<long, IReadOnlyList<OrderItem>>
        {
            [older.Id.Value] = [BuildItem(older.Id, productId)],
            [newer.Id.Value] = [BuildItem(newer.Id, productId)]
        });
        var service = new ReviewPurchaseVerificationService(orderRepo, itemRepo);

        var result = await service.GetVerifiedProductOrderIdAsync(userId, productId, CancellationToken.None);

        Assert.Equal(newer.Id.Value, result);
    }

    [Fact]
    public async Task VerifiedProductOrderId_Returns_Null_When_No_Eligible_Order()
    {
        var userId = Guid.NewGuid();
        var productId = ProductId.From(55);
        var cancelled = BuildOrder(1, userId, OrderStatus.Cancelled, DateTime.UtcNow);
        var orderRepo = new StubOrderRepository([cancelled]);
        var itemRepo = new StubOrderItemRepository(new Dictionary<long, IReadOnlyList<OrderItem>>
        {
            [cancelled.Id.Value] = [BuildItem(cancelled.Id, productId)]
        });
        var service = new ReviewPurchaseVerificationService(orderRepo, itemRepo);

        var result = await service.GetVerifiedProductOrderIdAsync(userId, productId, CancellationToken.None);

        Assert.Null(result);
    }

    private static Order BuildOrder(long id, Guid userId, OrderStatus status, DateTime createdAt)
        => Order.Reconstitute(
            OrderId.From(id),
            $"ORD-{id}",
            userId,
            CompanyId.From(Guid.NewGuid()),
            status,
            new Money(10),
            new Money(10),
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
            createdAt,
            createdAt,
            false,
            null);

    private static OrderItem BuildItem(OrderId orderId, ProductId productId)
        => OrderItem.Reconstitute(
            OrderItemId.From(1),
            orderId,
            productId,
            "Product",
            null,
            1,
            new Money(10),
            Money.Zero,
            new Money(10),
            CompanyId.From(Guid.NewGuid()),
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

    private sealed class StubOrderRepository : IOrderRepository
    {
        private readonly IReadOnlyList<Order> _orders;

        public StubOrderRepository(IReadOnlyList<Order> orders) => _orders = orders;

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) => Task.FromResult(_orders.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult(_orders.Where(x => x.CustomerId == customerId).ToList() as IReadOnlyList<Order>);
        public Task<(IReadOnlyList<Order> Items, long Total)> ListAsync(OrderListFilter filter, CancellationToken ct = default) => Task.FromResult<(IReadOnlyList<Order>, long)>(([], 0));
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) => Task.FromResult(order);
        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOrderItemRepository : IOrderItemRepository
    {
        private readonly IReadOnlyDictionary<long, IReadOnlyList<OrderItem>> _itemsByOrder;

        public StubOrderItemRepository(IReadOnlyDictionary<long, IReadOnlyList<OrderItem>> itemsByOrder) => _itemsByOrder = itemsByOrder;

        public Task<IReadOnlyList<OrderItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
            => Task.FromResult(_itemsByOrder.TryGetValue(orderId.Value, out var items) ? items : (IReadOnlyList<OrderItem>)[]);

        public Task AddRangeAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default) => Task.CompletedTask;
    }
}
