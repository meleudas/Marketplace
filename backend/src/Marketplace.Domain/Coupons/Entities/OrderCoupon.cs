using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Coupons.Entities;

public sealed class OrderCoupon : AuditableSoftDeleteAggregateRoot<OrderCouponId>
{
    private OrderCoupon() { }

    public OrderId OrderId { get; private set; } = null!;
    public CouponId CouponId { get; private set; } = null!;
    public Money DiscountApplied { get; private set; } = Money.Zero;
    public DateTime AppliedAt { get; private set; }

    public static OrderCoupon Reconstitute(
        OrderCouponId id,
        OrderId orderId,
        CouponId couponId,
        Money discountApplied,
        DateTime appliedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            CouponId = couponId,
            DiscountApplied = discountApplied,
            AppliedAt = appliedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
