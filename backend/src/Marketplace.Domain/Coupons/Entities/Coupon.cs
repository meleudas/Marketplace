using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Enums;
using CouponApplicableScope = Marketplace.Domain.Coupons.CouponApplicableScope;

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

    public bool IsValidAt(DateTime utcNow)
    {
        if (!IsActive || IsDeleted)
            return false;
        if (StartsAt.HasValue && utcNow < StartsAt.Value)
            return false;
        if (ExpiresAt.HasValue && utcNow > ExpiresAt.Value)
            return false;
        return true;
    }

    public bool IsUsageAvailableFor(Guid userId, int userUsageCount)
    {
        _ = userId;
        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value)
            return false;
        if (UserUsageLimit > 0 && userUsageCount >= UserUsageLimit)
            return false;
        return true;
    }

    public bool IsEligibleForSubtotal(Money subtotal)
        => MinOrderAmount is null || subtotal.Amount >= MinOrderAmount.Amount;

    public Money CalculateDiscount(Money subtotal)
    {
        if (subtotal.Amount <= 0)
            return Money.Zero;

        var discountAmount = DiscountType switch
        {
            DiscountType.Percentage => subtotal.Amount * (Discount.Amount / 100m),
            DiscountType.Fixed => Discount.Amount,
            _ => 0m
        };

        if (discountAmount < 0)
            discountAmount = 0;
        if (discountAmount > subtotal.Amount)
            discountAmount = subtotal.Amount;

        return new Money(discountAmount);
    }

    public bool IsCompanyInScope(Guid companyId)
        => CouponApplicableScope.ContainsGuid(ApplicableCompanies, companyId);

    public bool IsCategoryInScope(long categoryId)
        => CouponApplicableScope.ContainsLong(ApplicableCategories, categoryId);

    public bool IsProductInScope(long productId)
        => CouponApplicableScope.ContainsLong(ApplicableProducts, productId);

    public bool IsLineInScope(Guid companyId, long categoryId, long productId)
        => IsCompanyInScope(companyId) && IsCategoryInScope(categoryId) && IsProductInScope(productId);

    public void IncrementUsage(DateTime utcNow)
    {
        UsageCount += 1;
        UpdatedAt = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }

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
