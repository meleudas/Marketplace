using Marketplace.Application.Carts.Cache;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Carts.Mappings;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.AddCartItem;

public sealed class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;

    public AddCartItemCommandHandler(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IAppCachePort cache)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is null || product.Status != ProductStatus.Active || product.IsDeleted)
                return Result<CartDto>.Failure("Product not found");

            var now = DateTime.UtcNow;
            var cart = await _cartRepository.GetActiveByUserIdAsync(request.ActorUserId, ct);
            if (cart is null)
            {
                cart = Cart.Reconstitute(CartId.From(0), request.ActorUserId, CartStatus.Active, now, now, now, false, null);
                cart = await _cartRepository.AddAsync(cart, ct);
            }

            var existingItem = await _cartItemRepository.GetByCartAndProductAsync(cart.Id, product.Id, ct);
            if (existingItem is null)
            {
                var newItem = CartItem.Reconstitute(
                    CartItemId.From(0),
                    cart.Id,
                    product.Id,
                    request.Quantity,
                    product.Price,
                    Money.Zero,
                    now,
                    now,
                    false,
                    null);
                _ = await _cartItemRepository.AddAsync(newItem, ct);
            }
            else
            {
                var updated = CartItem.Reconstitute(
                    existingItem.Id,
                    existingItem.CartId,
                    existingItem.ProductId,
                    existingItem.Quantity + request.Quantity,
                    existingItem.PriceAtMoment,
                    existingItem.Discount,
                    existingItem.CreatedAt,
                    now,
                    existingItem.IsDeleted,
                    existingItem.DeletedAt);
                await _cartItemRepository.UpdateAsync(updated, ct);
            }

            cart = Cart.Reconstitute(cart.Id, cart.UserId, cart.Status, now, cart.CreatedAt, now, cart.IsDeleted, cart.DeletedAt);
            await _cartRepository.UpdateAsync(cart, ct);
            await _cache.RemoveAsync(CartCacheKeys.ActiveByUser(request.ActorUserId), ct);

            var items = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
            return Result<CartDto>.Success(CartMapping.ToDto(cart, items));
        }
        catch (Exception ex)
        {
            return Result<CartDto>.Failure($"Failed to add item to cart: {ex.Message}");
        }
    }
}
