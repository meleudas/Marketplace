using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;

namespace Marketplace.Domain.Orders.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
}
