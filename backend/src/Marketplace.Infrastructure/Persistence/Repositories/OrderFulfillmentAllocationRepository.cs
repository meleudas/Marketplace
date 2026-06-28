using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class OrderFulfillmentAllocationRepository : IOrderFulfillmentAllocationRepository
{
    private readonly ApplicationDbContext _context;

    public OrderFulfillmentAllocationRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<OrderFulfillmentAllocation>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.OrderFulfillmentAllocations.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .OrderBy(x => x.WarehouseId)
            .ThenBy(x => x.OrderItemId)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<OrderFulfillmentAllocation>> ListByOrderAndWarehouseAsync(
        OrderId orderId,
        WarehouseId warehouseId,
        CancellationToken ct = default)
    {
        var rows = await _context.OrderFulfillmentAllocations.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value && x.WarehouseId == warehouseId.Value)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddRangeAsync(IReadOnlyList<OrderFulfillmentAllocation> allocations, CancellationToken ct = default)
    {
        if (allocations.Count == 0)
            return;
        await _context.OrderFulfillmentAllocations.AddRangeAsync(allocations.Select(ToRecord), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OrderFulfillmentAllocation allocation, CancellationToken ct = default)
    {
        var row = await _context.OrderFulfillmentAllocations.FirstOrDefaultAsync(x => x.Id == allocation.Id.Value, ct)
            ?? throw new InvalidOperationException("Allocation not found");
        row.Status = (short)allocation.Status;
        row.ReservationId = allocation.ReservationId?.Value;
        row.UpdatedAt = allocation.UpdatedAt;
        row.IsDeleted = allocation.IsDeleted;
        row.DeletedAt = allocation.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IReadOnlyList<OrderFulfillmentAllocation> allocations, CancellationToken ct = default)
    {
        foreach (var allocation in allocations)
        {
            var row = await _context.OrderFulfillmentAllocations.FirstOrDefaultAsync(x => x.Id == allocation.Id.Value, ct)
                ?? throw new InvalidOperationException("Allocation not found");
            row.Status = (short)allocation.Status;
            row.ReservationId = allocation.ReservationId?.Value;
            row.UpdatedAt = allocation.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static OrderFulfillmentAllocation ToDomain(OrderFulfillmentAllocationRecord row) =>
        OrderFulfillmentAllocation.Reconstitute(
            OrderFulfillmentAllocationId.From(row.Id),
            OrderId.From(row.OrderId),
            OrderItemId.From(row.OrderItemId),
            CompanyId.From(row.CompanyId),
            WarehouseId.From(row.WarehouseId),
            ProductId.From(row.ProductId),
            row.Quantity,
            row.ReservationId is null ? null : InventoryReservationId.From(row.ReservationId.Value),
            (OrderFulfillmentAllocationStatus)row.Status,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static OrderFulfillmentAllocationRecord ToRecord(OrderFulfillmentAllocation x) =>
        new()
        {
            Id = x.Id.Value,
            OrderId = x.OrderId.Value,
            OrderItemId = x.OrderItemId.Value,
            CompanyId = x.CompanyId.Value,
            WarehouseId = x.WarehouseId.Value,
            ProductId = x.ProductId.Value,
            Quantity = x.Quantity,
            ReservationId = x.ReservationId?.Value,
            Status = (short)x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
