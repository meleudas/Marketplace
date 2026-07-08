using Marketplace.Application.Carts.Cache;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Carts.Mappings;
using Marketplace.Application.Carts.Services;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System.Text.Json;

namespace Marketplace.Application.Carts.Commands.AddCartItem;

public sealed class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly ICartStockWatchSyncService _cartStockWatchSync;
    private readonly IOutboxWriter _outbox;

    public AddCartItemCommandHandler(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IAppCachePort cache,
        ICartStockWatchSyncService cartStockWatchSync,
        IOutboxWriter? outbox = null)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _cache = cache;
        _cartStockWatchSync = cartStockWatchSync;
        _outbox = outbox ?? NoOpOutboxWriter.Instance;
    }

    public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken ct)
    {
        using var activity = MarketplaceTelemetry.StartActivity("cart.mutate");
        activity?.SetTag("operation", "add_item");

        try
        {
            if (request.Quantity <= 0)
                return Result<CartDto>.Failure("Quantity must be greater than zero");

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
            if (existingItem is not null)
            {
                if (existingItem.Quantity > int.MaxValue - request.Quantity)
                    return Result<CartDto>.Failure("Quantity overflow");
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
            else
            {
                var deletedItem = await _cartItemRepository.GetByCartAndProductIncludingDeletedAsync(cart.Id, product.Id, ct);
                if (deletedItem is not null && deletedItem.IsDeleted)
                {
                    await _cartItemRepository.ReactivateAsync(deletedItem.Id, request.Quantity, product.Price, now, ct);
                }
                else
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
            }

            cart = Cart.Reconstitute(cart.Id, cart.UserId, cart.Status, now, cart.CreatedAt, now, cart.IsDeleted, cart.DeletedAt);
            await _cartRepository.UpdateAsync(cart, ct);
            await _cache.RemoveAsync(CartCacheKeys.ActiveByUser(request.ActorUserId), ct);
            await _cartStockWatchSync.SyncWatchForUserCartProductAsync(request.ActorUserId, cart.Id, product.Id, ct);
            await PublishCartAddEventAsync(request.ActorUserId, request.ProductId, request.Quantity, ct);

            var items = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
            return Result<CartDto>.Success(CartMapping.ToDto(cart, items));
        }
        catch (Exception ex)
        {
            return Result<CartDto>.Failure($"Failed to add item to cart: {ex.Message}");
        }
    }

    private Task PublishCartAddEventAsync(Guid actorUserId, long productId, int quantity, CancellationToken ct)
    {
        var messageId = DomainEventIds.ForBehaviorEvent(DateTime.UtcNow.Ticks ^ productId ^ quantity);
        var payload = JsonSerializer.Serialize(new
        {
            messageId,
            eventId = 0,
            eventType = BehaviorEventType.AddToCart.ToString(),
            occurredAtUtc = DateTime.UtcNow,
            userId = actorUserId,
            sessionId = $"user:{actorUserId:N}",
            source = "cart:add",
            schemaVersion = 1,
            eventKey = $"cart|{actorUserId:N}|{productId}|{quantity}",
            payloadJson = JsonSerializer.Serialize(new { productId, quantity })
        });
        return _outbox.AppendAsync("BehaviorEvent", actorUserId.ToString("N"), "behavior.event.ingested", payload, ct);
    }
}
