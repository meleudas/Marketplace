using Marketplace.Application.Carts.Cache;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Carts.Mappings;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Carts.Queries.GetMyCart;

public sealed class GetMyCartQueryHandler : IRequestHandler<GetMyCartQuery, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetMyCartQueryHandler(ICartRepository cartRepository, ICartItemRepository cartItemRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<CartDto>> Handle(GetMyCartQuery request, CancellationToken ct)
    {
        try
        {
            var cacheKey = CartCacheKeys.ActiveByUser(request.ActorUserId);
            var cached = await _cache.GetAsync<CartDto>(cacheKey, ct);
            if (cached is not null)
                return Result<CartDto>.Success(cached);

            var cart = await _cartRepository.GetActiveByUserIdAsync(request.ActorUserId, ct);
            if (cart is null)
            {
                var now = DateTime.UtcNow;
                cart = Cart.Reconstitute(
                    CartId.From(0),
                    request.ActorUserId,
                    CartStatus.Active,
                    now,
                    now,
                    now,
                    false,
                    null);
                cart = await _cartRepository.AddAsync(cart, ct);
            }

            var items = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
            var dto = CartMapping.ToDto(cart, items);
            await _cache.SetAsync(cacheKey, dto, _ttl.Cart, ct);
            return Result<CartDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<CartDto>.Failure($"Failed to get cart: {ex.Message}");
        }
    }
}
