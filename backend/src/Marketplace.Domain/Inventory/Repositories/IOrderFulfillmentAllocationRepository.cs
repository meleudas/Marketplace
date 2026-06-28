using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;

namespace Marketplace.Domain.Inventory.Repositories;

public interface IOrderFulfillmentAllocationRepository
{
    Task<IReadOnlyList<OrderFulfillmentAllocation>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default);

    Task<IReadOnlyList<OrderFulfillmentAllocation>> ListByOrderAndWarehouseAsync(
        OrderId orderId,
        WarehouseId warehouseId,
        CancellationToken ct = default);

    Task AddRangeAsync(IReadOnlyList<OrderFulfillmentAllocation> allocations, CancellationToken ct = default);

    Task UpdateAsync(OrderFulfillmentAllocation allocation, CancellationToken ct = default);

    Task UpdateRangeAsync(IReadOnlyList<OrderFulfillmentAllocation> allocations, CancellationToken ct = default);
}
