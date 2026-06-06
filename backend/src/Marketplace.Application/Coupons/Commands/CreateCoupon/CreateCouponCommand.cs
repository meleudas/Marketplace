using Marketplace.Application.Coupons.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Enums;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Coupons.Commands.CreateCoupon;

public sealed record CreateCouponCommand(
    string Code,
    string? Description,
    decimal DiscountAmount,
    string DiscountType,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UserUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    string? ApplicableCompaniesJson) : IRequest<Result<CouponDto>>;

public sealed class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<CouponDto>>
{
    private readonly ICouponRepository _couponRepository;

    public CreateCouponCommandHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<Result<CouponDto>> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<DiscountType>(request.DiscountType, true, out var discountType))
            return Result<CouponDto>.Failure("unprocessable: invalid discount type");

        var existing = await _couponRepository.GetByCodeAsync(request.Code.Trim(), ct);
        if (existing is not null)
            return Result<CouponDto>.Failure("conflict: coupon code already exists");

        var now = DateTime.UtcNow;
        var coupon = Coupon.Reconstitute(
            CouponId.From(0),
            request.Code.Trim(),
            request.Description?.Trim(),
            new Money(request.DiscountAmount),
            discountType,
            request.MinOrderAmount.HasValue ? new Money(request.MinOrderAmount.Value) : null,
            request.UsageLimit,
            0,
            request.UserUsageLimit,
            request.ExpiresAtUtc,
            request.StartsAtUtc,
            null,
            null,
            string.IsNullOrWhiteSpace(request.ApplicableCompaniesJson) ? null : new JsonBlob(request.ApplicableCompaniesJson),
            request.IsActive,
            now,
            now,
            false,
            null);

        var saved = await _couponRepository.AddAsync(coupon, ct);
        return Result<CouponDto>.Success(saved.ToDto());
    }
}
