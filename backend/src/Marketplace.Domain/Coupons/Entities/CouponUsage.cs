using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Coupons.Entities;

public sealed class CouponUsage : AuditableSoftDeleteAggregateRoot<CouponUsageId>
{
    private CouponUsage() { }

    public CouponId CouponId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public OrderId OrderId { get; private set; } = null!;
    public string CouponCode { get; private set; } = string.Empty;
    public Money DiscountApplied { get; private set; } = Money.Zero;
    public DateTime ConsumedAtUtc { get; private set; }

    public static CouponUsage Reconstitute(
        CouponUsageId id,
        CouponId couponId,
        Guid userId,
        OrderId orderId,
        string couponCode,
        Money discountApplied,
        DateTime consumedAtUtc,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CouponId = couponId,
            UserId = userId,
            OrderId = orderId,
            CouponCode = couponCode,
            DiscountApplied = discountApplied,
            ConsumedAtUtc = consumedAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
