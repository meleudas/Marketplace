using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentRepository(ApplicationDbContext context) => _context = context;

    public async Task<Shipment?> GetByIdAsync(ShipmentId id, CancellationToken ct = default)
    {
        var row = await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Shipment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var row = await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Shipment>> ListByCustomerAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.Shipments.AsNoTracking()
            .Where(x => x.CustomerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Shipment> AddAsync(Shipment entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.Shipments.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Shipment entity, CancellationToken ct = default)
    {
        var row = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Shipment '{entity.Id.Value}' was not found.");

        row.OrderId = entity.OrderId.Value;
        row.CustomerId = entity.CustomerId;
        row.ShippingMethodId = entity.ShippingMethodId.Value;
        row.CarrierCode = (short)entity.CarrierCode;
        row.Status = (short)entity.Status;
        row.TrackingNumber = entity.TrackingNumber;
        row.LastSyncedAtUtc = entity.LastSyncedAtUtc;
        row.RawPayload = entity.RawPayload.Raw ?? "{}";
        row.UpdatedAt = entity.UpdatedAt;
        row.IsDeleted = entity.IsDeleted;
        row.DeletedAt = entity.DeletedAt;

        await _context.SaveChangesAsync(ct);
    }

    private static Shipment ToDomain(ShipmentRecord row) =>
        Shipment.Reconstitute(
            ShipmentId.From(row.Id),
            OrderId.From(row.OrderId),
            row.CustomerId,
            ShippingMethodId.From(row.ShippingMethodId),
            (ShippingCarrierCode)row.CarrierCode,
            (DeliveryStatus)row.Status,
            row.TrackingNumber,
            row.LastSyncedAtUtc,
            new JsonBlob(row.RawPayload),
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ShipmentRecord ToRecord(Shipment entity) =>
        new()
        {
            Id = entity.Id.Value,
            OrderId = entity.OrderId.Value,
            CustomerId = entity.CustomerId,
            ShippingMethodId = entity.ShippingMethodId.Value,
            CarrierCode = (short)entity.CarrierCode,
            Status = (short)entity.Status,
            TrackingNumber = entity.TrackingNumber,
            LastSyncedAtUtc = entity.LastSyncedAtUtc,
            RawPayload = entity.RawPayload.Raw ?? "{}",
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
