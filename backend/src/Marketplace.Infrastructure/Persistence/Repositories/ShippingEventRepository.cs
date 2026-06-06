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
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
        await _context.ShippingEvents.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);

        return ShippingEvent.Reconstitute(
            ShippingEventId.From(row.Id),
            (ShippingCarrierCode)row.CarrierCode,
            row.EventKey,
            row.PayloadHash,
            new JsonBlob(row.RawPayload),
            row.ReceivedAtUtc,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
    }
}
