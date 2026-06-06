using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Enums;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CouponRepository : ICouponRepository
{
    private readonly ApplicationDbContext _context;

    public CouponRepository(ApplicationDbContext context) => _context = context;

    public async Task<Coupon?> GetByIdAsync(CouponId id, CancellationToken ct = default)
    {
        var row = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim();
        var row = await _context.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalized, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Coupon> AddAsync(Coupon entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.Coupons.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Coupon entity, CancellationToken ct = default)
    {
        var row = await _context.Coupons.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Coupon '{entity.Id.Value}' was not found.");

        row.Code = entity.Code;
        row.Description = entity.Description;
        row.DiscountAmount = entity.Discount.Amount;
        row.DiscountType = (short)entity.DiscountType;
        row.MinOrderAmount = entity.MinOrderAmount?.Amount;
        row.UsageLimit = entity.UsageLimit;
        row.UsageCount = entity.UsageCount;
        row.UserUsageLimit = entity.UserUsageLimit;
        row.ExpiresAtUtc = entity.ExpiresAt;
        row.StartsAtUtc = entity.StartsAt;
        row.ApplicableCategoriesRaw = entity.ApplicableCategories?.Raw;
        row.ApplicableProductsRaw = entity.ApplicableProducts?.Raw;
        row.ApplicableCompaniesRaw = entity.ApplicableCompanies?.Raw;
        row.IsActive = entity.IsActive;
        row.UpdatedAt = entity.UpdatedAt;
        row.IsDeleted = entity.IsDeleted;
        row.DeletedAt = entity.DeletedAt;

        await _context.SaveChangesAsync(ct);
    }

    private static Coupon ToDomain(CouponRecord row) =>
        Coupon.Reconstitute(
            CouponId.From(row.Id),
            row.Code,
            row.Description,
            new Money(row.DiscountAmount),
            (DiscountType)row.DiscountType,
            row.MinOrderAmount.HasValue ? new Money(row.MinOrderAmount.Value) : null,
            row.UsageLimit,
            row.UsageCount,
            row.UserUsageLimit,
            row.ExpiresAtUtc,
            row.StartsAtUtc,
            row.ApplicableCategoriesRaw is null ? null : new JsonBlob(row.ApplicableCategoriesRaw),
            row.ApplicableProductsRaw is null ? null : new JsonBlob(row.ApplicableProductsRaw),
            row.ApplicableCompaniesRaw is null ? null : new JsonBlob(row.ApplicableCompaniesRaw),
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CouponRecord ToRecord(Coupon entity) =>
        new()
        {
            Id = entity.Id.Value,
            Code = entity.Code,
            Description = entity.Description,
            DiscountAmount = entity.Discount.Amount,
            DiscountType = (short)entity.DiscountType,
            MinOrderAmount = entity.MinOrderAmount?.Amount,
            UsageLimit = entity.UsageLimit,
            UsageCount = entity.UsageCount,
            UserUsageLimit = entity.UserUsageLimit,
            ExpiresAtUtc = entity.ExpiresAt,
            StartsAtUtc = entity.StartsAt,
            ApplicableCategoriesRaw = entity.ApplicableCategories?.Raw,
            ApplicableProductsRaw = entity.ApplicableProducts?.Raw,
            ApplicableCompaniesRaw = entity.ApplicableCompanies?.Raw,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
