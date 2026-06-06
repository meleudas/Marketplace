using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Coupons.Commands.DeactivateCoupon;

public sealed record DeactivateCouponCommand(long CouponId) : IRequest<Result>;

public sealed class DeactivateCouponCommandHandler : IRequestHandler<DeactivateCouponCommand, Result>
{
    private readonly ICouponRepository _couponRepository;

    public DeactivateCouponCommandHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<Result> Handle(DeactivateCouponCommand request, CancellationToken ct)
    {
        var coupon = await _couponRepository.GetByIdAsync(CouponId.From(request.CouponId), ct);
        if (coupon is null)
            return Result.Failure("not found");

        coupon.Deactivate(DateTime.UtcNow);
        await _couponRepository.UpdateAsync(coupon, ct);
        return Result.Success();
    }
}
