using Marketplace.Application.Coupons.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Enums;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Coupons.Commands.UpdateCoupon;

public sealed record UpdateCouponCommand(
    long CouponId,
    string? Description,
    decimal DiscountAmount,
    string DiscountType,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UserUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    string? ApplicableCompaniesJson,
    string? ApplicableCategoriesJson,
    string? ApplicableProductsJson) : IRequest<Result<CouponDto>>;

public sealed class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Result<CouponDto>>
{
    private readonly ICouponRepository _couponRepository;

    public UpdateCouponCommandHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<Result<CouponDto>> Handle(UpdateCouponCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<DiscountType>(request.DiscountType, true, out var discountType))
            return Result<CouponDto>.Failure("unprocessable: invalid discount type");

        var existing = await _couponRepository.GetByIdAsync(CouponId.From(request.CouponId), ct);
        if (existing is null)
            return Result<CouponDto>.Failure("not found");

        var now = DateTime.UtcNow;
        var userUsageLimit = request.UserUsageLimit <= 0 ? 1 : request.UserUsageLimit;
        var updated = Coupon.Reconstitute(
            existing.Id,
            existing.Code,
            request.Description?.Trim(),
            new Money(request.DiscountAmount),
            discountType,
            request.MinOrderAmount.HasValue ? new Money(request.MinOrderAmount.Value) : null,
            request.UsageLimit,
            existing.UsageCount,
            userUsageLimit,
            request.ExpiresAtUtc,
            request.StartsAtUtc,
            string.IsNullOrWhiteSpace(request.ApplicableCategoriesJson)
                ? existing.ApplicableCategories
                : new JsonBlob(request.ApplicableCategoriesJson),
            string.IsNullOrWhiteSpace(request.ApplicableProductsJson)
                ? existing.ApplicableProducts
                : new JsonBlob(request.ApplicableProductsJson),
            string.IsNullOrWhiteSpace(request.ApplicableCompaniesJson) ? null : new JsonBlob(request.ApplicableCompaniesJson),
            request.IsActive,
            existing.CreatedAt,
            now,
            existing.IsDeleted,
            existing.DeletedAt);

        await _couponRepository.UpdateAsync(updated, ct);
        return Result<CouponDto>.Success(updated.ToDto());
    }
}
