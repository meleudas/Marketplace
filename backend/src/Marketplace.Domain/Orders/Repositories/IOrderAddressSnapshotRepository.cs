using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Domain.Orders.Repositories;

public interface IOrderAddressSnapshotRepository
{
    Task<IReadOnlyList<OrderAddressSnapshot>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<OrderAddressSnapshot> addresses, CancellationToken ct = default);
}
