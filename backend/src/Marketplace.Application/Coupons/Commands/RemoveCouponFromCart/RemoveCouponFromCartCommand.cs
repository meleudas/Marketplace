using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Coupons.Commands.RemoveCouponFromCart;

public sealed record RemoveCouponFromCartCommand(Guid ActorUserId, string Code) : IRequest<Result>;

public sealed class RemoveCouponFromCartCommandHandler : IRequestHandler<RemoveCouponFromCartCommand, Result>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartCouponLinkRepository _cartCouponLinkRepository;

    public RemoveCouponFromCartCommandHandler(ICartRepository cartRepository, ICartCouponLinkRepository cartCouponLinkRepository)
    {
        _cartRepository = cartRepository;
        _cartCouponLinkRepository = cartCouponLinkRepository;
    }

    public async Task<Result> Handle(RemoveCouponFromCartCommand request, CancellationToken ct)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(request.ActorUserId, ct);
        if (cart is null)
            return Result.Failure("not found");

        var link = await _cartCouponLinkRepository.GetByCartIdAsync(cart.Id, ct);
        if (link is null || !string.Equals(link.CouponCode, request.Code, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("not found");

        await _cartCouponLinkRepository.RemoveByCartIdAsync(cart.Id, ct);
        return Result.Success();
    }
}
