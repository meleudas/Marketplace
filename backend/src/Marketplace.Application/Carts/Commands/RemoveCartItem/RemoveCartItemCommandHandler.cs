using Marketplace.Application.Carts.Cache;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Carts.Mappings;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IAppCachePort _cache;

    public RemoveCartItemCommandHandler(ICartRepository cartRepository, ICartItemRepository cartItemRepository, IAppCachePort cache)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _cache = cache;
    }

    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        try
        {
            var cart = await _cartRepository.GetActiveByUserIdAsync(request.ActorUserId, ct);
            if (cart is null)
                return Result<CartDto>.Failure("Cart not found");

            var item = await _cartItemRepository.GetByIdAsync(CartItemId.From(request.CartItemId), ct);
            if (item is null || item.CartId != cart.Id)
                return Result<CartDto>.Failure("Cart item not found");

            var now = DateTime.UtcNow;
            await _cartItemRepository.SoftDeleteAsync(item.Id, now, ct);

            cart = Cart.Reconstitute(cart.Id, cart.UserId, cart.Status, now, cart.CreatedAt, now, cart.IsDeleted, cart.DeletedAt);
            await _cartRepository.UpdateAsync(cart, ct);
            await _cache.RemoveAsync(CartCacheKeys.ActiveByUser(request.ActorUserId), ct);

            var items = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
            return Result<CartDto>.Success(CartMapping.ToDto(cart, items));
        }
        catch (Exception ex)
        {
            return Result<CartDto>.Failure($"Failed to remove cart item: {ex.Message}");
        }
    }
}
