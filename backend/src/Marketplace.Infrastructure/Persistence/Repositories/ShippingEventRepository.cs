using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ShippingEventRepository : IShippingEventRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingEventRepository(ApplicationDbContext context) => _context = context;

    public async Task<bool> ExistsByDedupAsync(ShippingCarrierCode carrierCode, string eventKey, string payloadHash, CancellationToken ct = default)
        => await _context.ShippingEvents.AsNoTracking()
            .AnyAsync(x => x.CarrierCode == (short)carrierCode && x.EventKey == eventKey && x.PayloadHash == payloadHash, ct);

    public async Task<IReadOnlyList<ShippingEvent>> ListByShipmentIdAsync(ShipmentId shipmentId, CancellationToken ct = default)
    {
        var rows = await _context.ShippingEvents.AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId.Value)
            .OrderBy(x => x.OccurredAtUtc ?? x.ReceivedAtUtc)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<ShippingEvent> AddAsync(ShippingEvent entity, CancellationToken ct = default)
    {
        var row = new ShippingEventRecord
        {
            Id = entity.Id.Value,
            CarrierCode = (short)entity.CarrierCode,
            EventKey = entity.EventKey,
            PayloadHash = entity.PayloadHash,
            RawPayload = entity.RawPayload.Raw ?? "{}",
            ReceivedAtUtc = entity.ReceivedAtUtc,
            ShipmentId = entity.ShipmentId?.Value,
            OrderId = entity.OrderId?.Value,
            TrackingNumber = entity.TrackingNumber,
            DeliveryStatus = entity.DeliveryStatus.HasValue ? (short)entity.DeliveryStatus.Value : null,
            OccurredAtUtc = entity.OccurredAtUtc,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
        await _context.ShippingEvents.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    private static ShippingEvent ToDomain(ShippingEventRecord row) =>
        ShippingEvent.Reconstitute(
            ShippingEventId.From(row.Id),
            (ShippingCarrierCode)row.CarrierCode,
            row.EventKey,
            row.PayloadHash,
            new JsonBlob(row.RawPayload),
            row.ReceivedAtUtc,
            row.ShipmentId.HasValue ? ShipmentId.From(row.ShipmentId.Value) : null,
            row.OrderId.HasValue ? OrderId.From(row.OrderId.Value) : null,
            row.TrackingNumber,
            row.DeliveryStatus.HasValue ? (DeliveryStatus)row.DeliveryStatus.Value : null,
            row.OccurredAtUtc,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
}
