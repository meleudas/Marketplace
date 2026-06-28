using Marketplace.Application.Orders.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public sealed class ApplicationOrderStatusHistoryWriterTests
{
    [Fact]
    public async Task RecordCreatedAsync_Is_Idempotent_For_Same_Source()
    {
        var repo = new InMemoryHistoryRepository();
        var writer = new OrderStatusHistoryWriter(repo);
        var order = BuildOrder();

        await writer.RecordCreatedAsync(order, Guid.NewGuid(), "checkout", "ORD-1", CancellationToken.None);
        await writer.RecordCreatedAsync(order, Guid.NewGuid(), "checkout", "ORD-1", CancellationToken.None);

        Assert.Single(repo.Entries);
        Assert.Equal("created", repo.Entries[0].Comment);
    }

    private static Order BuildOrder() =>
        Order.Reconstitute(
            OrderId.From(5),
            "ORD-5",
            Guid.NewGuid(),
            CompanyId.From(Guid.NewGuid()),
            OrderStatus.Pending,
            new Money(10),
            new Money(10),
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

    private sealed class InMemoryHistoryRepository : IOrderStatusHistoryRepository
    {
        public List<OrderStatusHistory> Entries { get; } = [];

        public Task AddAsync(OrderStatusHistory history, CancellationToken ct = default)
        {
            Entries.Add(history);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OrderStatusHistory>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OrderStatusHistory>>(Entries.Where(x => x.OrderId == orderId).ToList());
    }
}
