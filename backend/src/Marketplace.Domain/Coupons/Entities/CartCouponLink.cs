using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Coupons.Entities;

public sealed class CartCouponLink : AuditableSoftDeleteAggregateRoot<CartCouponLinkId>
{
    private CartCouponLink() { }

    public CartId CartId { get; private set; } = null!;
    public CouponId CouponId { get; private set; } = null!;
    public string CouponCode { get; private set; } = string.Empty;
    public DateTime AppliedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public JsonBlob ValidationSnapshot { get; private set; } = JsonBlob.Empty;

    public bool IsExpired(DateTime utcNow)
        => ExpiresAtUtc.HasValue && utcNow >= ExpiresAtUtc.Value;

    public static CartCouponLink Reconstitute(
        CartCouponLinkId id,
        CartId cartId,
        CouponId couponId,
        string couponCode,
        DateTime appliedAtUtc,
        DateTime? expiresAtUtc,
        JsonBlob validationSnapshot,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CartId = cartId,
            CouponId = couponId,
            CouponCode = couponCode,
            AppliedAtUtc = appliedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            ValidationSnapshot = validationSnapshot,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
