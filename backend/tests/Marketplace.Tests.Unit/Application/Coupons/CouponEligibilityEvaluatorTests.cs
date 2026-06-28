using Marketplace.Application.Coupons.Validation;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Enums;
using Marketplace.Domain.Coupons.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Coupons")]
public sealed class CouponEligibilityEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_Fails_When_No_Eligible_Category()
    {
        var companyId = Guid.NewGuid();
        var coupon = BuildCoupon(companyId, categoriesJson: "[2]");
        var evaluator = CreateEvaluator(new InMemoryUsageRepo());
        var lines = new List<CouponCartLine>
        {
            new(100, 1, companyId, 1, 50m),
            new(101, 1, companyId, 1, 50m)
        };

        var result = await evaluator.EvaluateAsync(coupon, Guid.NewGuid(), lines);

        Assert.False(result.IsValid);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateAsync_Discounts_Only_Eligible_Category_Lines()
    {
        var companyId = Guid.NewGuid();
        var coupon = BuildCoupon(companyId, categoriesJson: "[2]", discountAmount: 10, discountType: DiscountType.Percentage);
        var evaluator = CreateEvaluator(new InMemoryUsageRepo());
        var lines = new List<CouponCartLine>
        {
            new(100, 1, companyId, 1, 100m),
            new(101, 2, companyId, 1, 100m)
        };

        var result = await evaluator.EvaluateAsync(coupon, Guid.NewGuid(), lines);

        Assert.True(result.IsValid);
        Assert.Equal(200m, result.CartSubtotal);
        Assert.Equal(100m, result.EligibleSubtotal);
        Assert.Equal(10m, result.DiscountAmount);
    }

    [Fact]
    public void AllocateDiscount_Splits_Proportionally_Across_Companies()
    {
        var byCompany = new Dictionary<Guid, decimal>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = 60m,
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = 40m
        };

        var allocated = CouponEligibilityEvaluator.AllocateDiscount(20m, byCompany);

        Assert.Equal(12m, allocated[Guid.Parse("11111111-1111-1111-1111-111111111111")]);
        Assert.Equal(8m, allocated[Guid.Parse("22222222-2222-2222-2222-222222222222")]);
        Assert.Equal(20m, allocated.Values.Sum());
    }

    private static CouponEligibilityEvaluator CreateEvaluator(ICouponUsageRepository usageRepo) =>
        new(usageRepo,
        [
            new ActiveWindowCouponRule(),
            new CompanyScopeCouponRule(),
            new UsageLimitsCouponRule(),
            new MinOrderAmountCouponRule()
        ]);

    private static Coupon BuildCoupon(
        Guid companyId,
        string? categoriesJson = null,
        decimal discountAmount = 20,
        DiscountType discountType = DiscountType.Fixed)
    {
        var now = DateTime.UtcNow;
        return Coupon.Reconstitute(
            CouponId.From(1),
            "SCOPE10",
            null,
            new Money(discountAmount),
            discountType,
            null,
            100,
            0,
            1,
            now.AddDays(7),
            now.AddDays(-1),
            string.IsNullOrWhiteSpace(categoriesJson) ? null : new JsonBlob(categoriesJson),
            null,
            new JsonBlob($"[\"{companyId}\"]"),
            true,
            now,
            now,
            false,
            null);
    }

    private sealed class InMemoryUsageRepo : ICouponUsageRepository
    {
        public Task<int> CountByCouponAndUserAsync(CouponId couponId, Guid userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> ExistsByCouponAndOrderAsync(CouponId couponId, OrderId orderId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<CouponUsage> AddAsync(CouponUsage entity, CancellationToken ct = default) => Task.FromResult(entity);
    }
}
