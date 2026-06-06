namespace Marketplace.Application.Coupons.DTOs;

public sealed record CouponDto(
    long Id,
    string Code,
    string? Description,
    decimal DiscountAmount,
    string DiscountType,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UsageCount,
    int UserUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive);

public sealed record CouponValidationResultDto(
    bool IsValid,
    string? ErrorCode,
    string? Message,
    string? Code,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAfterDiscount);

public sealed record CouponUsageReportDto(
    long CouponId,
    string Code,
    int UsageCount);

public sealed record CartCouponDto(
    long CartId,
    long CouponId,
    string Code,
    DateTime AppliedAtUtc,
    DateTime? ExpiresAtUtc);
