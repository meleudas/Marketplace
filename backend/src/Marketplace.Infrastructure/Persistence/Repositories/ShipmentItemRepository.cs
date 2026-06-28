using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ShipmentItemRepository : IShipmentItemRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentItemRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ShipmentItem>> ListByShipmentIdAsync(ShipmentId shipmentId, CancellationToken ct = default)
    {
        var rows = await _context.ShipmentItems.AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId.Value)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ShipmentItem>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var shipmentIds = await _context.Shipments.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (shipmentIds.Count == 0)
            return [];

        var rows = await _context.ShipmentItems.AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddRangeAsync(IReadOnlyList<ShipmentItem> items, CancellationToken ct = default)
    {
        if (items.Count == 0)
            return;

        var nextId = await _context.ShipmentItems.IgnoreQueryFilters()
            .MaxAsync(x => (long?)x.Id, ct) ?? 0L;

        foreach (var item in items)
        {
            var id = item.Id.Value;
            if (id <= 0)
                id = ++nextId;

            await _context.ShipmentItems.AddAsync(new ShipmentItemRecord
            {
                Id = id,
                ShipmentId = item.ShipmentId.Value,
                OrderItemId = item.OrderItemId.Value,
                Quantity = item.Quantity,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                IsDeleted = item.IsDeleted,
                DeletedAt = item.DeletedAt
            }, ct);
        }

        await _context.SaveChangesAsync(ct);
    }

    private static ShipmentItem ToDomain(ShipmentItemRecord row) =>
        ShipmentItem.Reconstitute(
            ShipmentItemId.From(row.Id),
            ShipmentId.From(row.ShipmentId),
            OrderItemId.From(row.OrderItemId),
            row.Quantity,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
}
