using Marketplace.Application.Coupons.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Coupons.Queries.GetCouponUsageReport;

public sealed record GetCouponUsageReportQuery(long CouponId) : IRequest<Result<CouponUsageReportDto>>;

public sealed class GetCouponUsageReportQueryHandler : IRequestHandler<GetCouponUsageReportQuery, Result<CouponUsageReportDto>>
{
    private readonly ICouponRepository _couponRepository;

    public GetCouponUsageReportQueryHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<Result<CouponUsageReportDto>> Handle(GetCouponUsageReportQuery request, CancellationToken ct)
    {
        var coupon = await _couponRepository.GetByIdAsync(CouponId.From(request.CouponId), ct);
        if (coupon is null)
            return Result<CouponUsageReportDto>.Failure("not found");

        return Result<CouponUsageReportDto>.Success(new CouponUsageReportDto(coupon.Id.Value, coupon.Code, coupon.UsageCount));
    }
}
