using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingMethodRepository(ApplicationDbContext context) => _context = context;

    public async Task<ShippingMethod?> GetByIdAsync(ShippingMethodId id, CancellationToken ct = default)
    {
        var row = await _context.ShippingMethods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<ShippingMethod>> ListActiveAsync(CancellationToken ct = default)
    {
        var rows = await _context.ShippingMethods.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Price)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    private static ShippingMethod ToDomain(ShippingMethodRecord row) =>
        ShippingMethod.Reconstitute(
            ShippingMethodId.From(row.Id),
            row.Name,
            (ShippingCarrierCode)row.Code,
            new Money(row.Price),
            row.FreeShippingThreshold.HasValue ? new Money(row.FreeShippingThreshold.Value) : null,
            row.EstimatedDaysMin,
            row.EstimatedDaysMax,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
}
