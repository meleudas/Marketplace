using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CouponUsageRepository : ICouponUsageRepository
{
    private readonly ApplicationDbContext _context;

    public CouponUsageRepository(ApplicationDbContext context) => _context = context;

    public Task<int> CountByCouponAndUserAsync(CouponId couponId, Guid userId, CancellationToken ct = default)
        => _context.CouponUsages.AsNoTracking().CountAsync(x => x.CouponId == couponId.Value && x.UserId == userId, ct);

    public Task<bool> ExistsByCouponAndOrderAsync(CouponId couponId, OrderId orderId, CancellationToken ct = default)
        => _context.CouponUsages.AsNoTracking().AnyAsync(x => x.CouponId == couponId.Value && x.OrderId == orderId.Value, ct);

    public async Task<CouponUsage> AddAsync(CouponUsage entity, CancellationToken ct = default)
    {
        var row = new CouponUsageRecord
        {
            Id = entity.Id.Value,
            CouponId = entity.CouponId.Value,
            UserId = entity.UserId,
            OrderId = entity.OrderId.Value,
            CouponCode = entity.CouponCode,
            DiscountAppliedAmount = entity.DiscountApplied.Amount,
            ConsumedAtUtc = entity.ConsumedAtUtc,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };

        await _context.CouponUsages.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);

        return CouponUsage.Reconstitute(
            CouponUsageId.From(row.Id),
            CouponId.From(row.CouponId),
            row.UserId,
            OrderId.From(row.OrderId),
            row.CouponCode,
            new Money(row.DiscountAppliedAmount),
            row.ConsumedAtUtc,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
    }
}
