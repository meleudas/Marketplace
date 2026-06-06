using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Application.Favorites.DTOs;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Favorites.Entities;
using Marketplace.Domain.Favorites.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Favorites.Commands.AddFavoriteProduct;

public sealed class AddFavoriteProductCommandHandler : IRequestHandler<AddFavoriteProductCommand, Result<FavoriteItemDto>>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;

    public AddFavoriteProductCommandHandler(IFavoriteRepository favoriteRepository, IProductRepository productRepository, IAppCachePort cache)
    {
        _favoriteRepository = favoriteRepository;
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<Result<FavoriteItemDto>> Handle(AddFavoriteProductCommand request, CancellationToken ct)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product = await _productRepository.GetByIdAsync(productId, ct);
            if (product is null || product.Status != ProductStatus.Active || product.IsDeleted)
                return Result<FavoriteItemDto>.Failure("Product not found");

            var existing = await _favoriteRepository.GetByUserAndProductAsync(request.ActorUserId, productId, ct);
            if (existing is not null)
            {
                return Result<FavoriteItemDto>.Success(
                    new FavoriteItemDto(existing.Id.Value, existing.ProductId.Value, existing.AddedAt, existing.PriceAtAdd?.Amount, existing.IsAvailable));
            }

            var now = DateTime.UtcNow;
            var deleted = await _favoriteRepository.GetByUserAndProductIncludingDeletedAsync(request.ActorUserId, productId, ct);
            if (deleted is not null && deleted.IsDeleted)
            {
                await _favoriteRepository.ReactivateAsync(deleted.Id, now, product.Price, ct);
                await _cache.RemoveAsync(FavoritesCacheKeys.ListByUser(request.ActorUserId), ct);
                var restored = await _favoriteRepository.GetByUserAndProductAsync(request.ActorUserId, productId, ct);
                if (restored is not null)
                {
                    return Result<FavoriteItemDto>.Success(
                        new FavoriteItemDto(restored.Id.Value, restored.ProductId.Value, restored.AddedAt, restored.PriceAtAdd?.Amount, restored.IsAvailable));
                }
            }

            var favorite = Favorite.Create(
                FavoriteId.From(0),
                request.ActorUserId,
                productId,
                now,
                product.Price,
                true,
                JsonBlob.Empty,
                null);

            try
            {
                favorite = await _favoriteRepository.AddAsync(favorite, ct);
            }
            catch (InvalidOperationException)
            {
                // Concurrent add can hit unique index; treat as idempotent success.
                var concurrent = await _favoriteRepository.GetByUserAndProductAsync(request.ActorUserId, productId, ct);
                if (concurrent is null)
                    return Result<FavoriteItemDto>.Failure("Failed to add favorite");

                await _cache.RemoveAsync(FavoritesCacheKeys.ListByUser(request.ActorUserId), ct);
                return Result<FavoriteItemDto>.Success(
                    new FavoriteItemDto(concurrent.Id.Value, concurrent.ProductId.Value, concurrent.AddedAt, concurrent.PriceAtAdd?.Amount, concurrent.IsAvailable));
            }

            await _cache.RemoveAsync(FavoritesCacheKeys.ListByUser(request.ActorUserId), ct);
            return Result<FavoriteItemDto>.Success(
                new FavoriteItemDto(favorite.Id.Value, favorite.ProductId.Value, favorite.AddedAt, favorite.PriceAtAdd?.Amount, favorite.IsAvailable));
        }
        catch
        {
            return Result<FavoriteItemDto>.Failure("Failed to add favorite");
        }
    }
}
