using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;

namespace Marketplace.Domain.Coupons.Repositories;

public interface ICouponUsageRepository
{
    Task<int> CountByCouponAndUserAsync(CouponId couponId, Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByCouponAndOrderAsync(CouponId couponId, OrderId orderId, CancellationToken ct = default);
    Task<CouponUsage> AddAsync(CouponUsage entity, CancellationToken ct = default);
}
