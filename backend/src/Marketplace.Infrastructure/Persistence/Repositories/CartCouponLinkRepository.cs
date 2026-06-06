using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CartCouponLinkRepository : ICartCouponLinkRepository
{
    private readonly ApplicationDbContext _context;

    public CartCouponLinkRepository(ApplicationDbContext context) => _context = context;

    public async Task<CartCouponLink?> GetByCartIdAsync(CartId cartId, CancellationToken ct = default)
    {
        var row = await _context.CartCouponLinks.AsNoTracking().FirstOrDefaultAsync(x => x.CartId == cartId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CartCouponLink> UpsertAsync(CartCouponLink entity, CancellationToken ct = default)
    {
        var existing = await _context.CartCouponLinks.FirstOrDefaultAsync(x => x.CartId == entity.CartId.Value, ct);
        if (existing is null)
        {
            var created = ToRecord(entity);
            await _context.CartCouponLinks.AddAsync(created, ct);
            await _context.SaveChangesAsync(ct);
            return ToDomain(created);
        }

        existing.CouponId = entity.CouponId.Value;
        existing.CouponCode = entity.CouponCode;
        existing.AppliedAtUtc = entity.AppliedAtUtc;
        existing.ExpiresAtUtc = entity.ExpiresAtUtc;
        existing.ValidationSnapshotRaw = entity.ValidationSnapshot.Raw ?? "{}";
        existing.UpdatedAt = entity.UpdatedAt;
        existing.IsDeleted = entity.IsDeleted;
        existing.DeletedAt = entity.DeletedAt;
        await _context.SaveChangesAsync(ct);
        return ToDomain(existing);
    }

    public async Task RemoveByCartIdAsync(CartId cartId, CancellationToken ct = default)
    {
        var existing = await _context.CartCouponLinks.FirstOrDefaultAsync(x => x.CartId == cartId.Value, ct);
        if (existing is null)
            return;

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    private static CartCouponLink ToDomain(CartCouponLinkRecord row) =>
        CartCouponLink.Reconstitute(
            CartCouponLinkId.From(row.Id),
            CartId.From(row.CartId),
            CouponId.From(row.CouponId),
            row.CouponCode,
            row.AppliedAtUtc,
            row.ExpiresAtUtc,
            new JsonBlob(row.ValidationSnapshotRaw),
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CartCouponLinkRecord ToRecord(CartCouponLink entity) =>
        new()
        {
            Id = entity.Id.Value,
            CartId = entity.CartId.Value,
            CouponId = entity.CouponId.Value,
            CouponCode = entity.CouponCode,
            AppliedAtUtc = entity.AppliedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            ValidationSnapshotRaw = entity.ValidationSnapshot.Raw ?? "{}",
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
