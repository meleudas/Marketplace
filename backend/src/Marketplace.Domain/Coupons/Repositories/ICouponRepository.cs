using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;

namespace Marketplace.Domain.Coupons.Repositories;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(CouponId id, CancellationToken ct = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Coupon> AddAsync(Coupon entity, CancellationToken ct = default);
    Task UpdateAsync(Coupon entity, CancellationToken ct = default);
}
