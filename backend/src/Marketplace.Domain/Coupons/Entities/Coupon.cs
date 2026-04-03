using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Enums;

namespace Marketplace.Domain.Coupons.Entities;

public sealed class Coupon : AuditableSoftDeleteAggregateRoot<CouponId>
{
    private Coupon() { }

    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money Discount { get; private set; } = Money.Zero;
    public DiscountType DiscountType { get; private set; }
    public Money? MinOrderAmount { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }
    public int UserUsageLimit { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public JsonBlob? ApplicableCategories { get; private set; }
    public JsonBlob? ApplicableProducts { get; private set; }
    public JsonBlob? ApplicableCompanies { get; private set; }
    public bool IsActive { get; private set; }

    public static Coupon Reconstitute(
        CouponId id,
        string code,
        string? description,
        Money discount,
        DiscountType discountType,
        Money? minOrderAmount,
        int? usageLimit,
        int usageCount,
        int userUsageLimit,
        DateTime? expiresAt,
        DateTime? startsAt,
        JsonBlob? applicableCategories,
        JsonBlob? applicableProducts,
        JsonBlob? applicableCompanies,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            Code = code,
            Description = description,
            Discount = discount,
            DiscountType = discountType,
            MinOrderAmount = minOrderAmount,
            UsageLimit = usageLimit,
            UsageCount = usageCount,
            UserUsageLimit = userUsageLimit,
            ExpiresAt = expiresAt,
            StartsAt = startsAt,
            ApplicableCategories = applicableCategories,
            ApplicableProducts = applicableProducts,
            ApplicableCompanies = applicableCompanies,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
