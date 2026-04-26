using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Domain.Orders.Repositories;

public interface IOrderStatusHistoryRepository
{
    Task AddAsync(OrderStatusHistory history, CancellationToken ct = default);
    Task<IReadOnlyList<OrderStatusHistory>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
}
