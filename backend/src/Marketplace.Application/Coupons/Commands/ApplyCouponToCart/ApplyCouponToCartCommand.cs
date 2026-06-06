using Marketplace.Application.Coupons.DTOs;
using Marketplace.Application.Coupons.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System.Text.Json;

namespace Marketplace.Application.Coupons.Commands.ApplyCouponToCart;

public sealed record ApplyCouponToCartCommand(Guid ActorUserId, string Code) : IRequest<Result<CartCouponDto>>;

public sealed class ApplyCouponToCartCommandHandler : IRequestHandler<ApplyCouponToCartCommand, Result<CartCouponDto>>
{
    private readonly CouponCartValidationService _validationService;
    private readonly ICartCouponLinkRepository _cartCouponLinkRepository;

    public ApplyCouponToCartCommandHandler(
        CouponCartValidationService validationService,
        ICartCouponLinkRepository cartCouponLinkRepository)
    {
        _validationService = validationService;
        _cartCouponLinkRepository = cartCouponLinkRepository;
    }

    public async Task<Result<CartCouponDto>> Handle(ApplyCouponToCartCommand request, CancellationToken ct)
    {
        var (validation, coupon, cartId) = await _validationService.ValidateAsync(request.ActorUserId, request.Code, ct);
        if (!validation.IsValid || coupon is null || cartId is null)
            return Result<CartCouponDto>.Failure($"{validation.ErrorCode ?? "unprocessable"}: {validation.Message}");

        var now = DateTime.UtcNow;
        var link = CartCouponLink.Reconstitute(
            CartCouponLinkId.From(0),
            cartId,
            coupon.Id,
            coupon.Code,
            now,
            coupon.ExpiresAt,
            new JsonBlob(JsonSerializer.Serialize(validation)),
            now,
            now,
            false,
            null);

        var saved = await _cartCouponLinkRepository.UpsertAsync(link, ct);
        return Result<CartCouponDto>.Success(saved.ToDto());
    }
}
