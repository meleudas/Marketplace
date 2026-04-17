using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Domain.Orders.Repositories;

public interface IOrderItemRepository
{
    Task<IReadOnlyList<OrderItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default);
}
