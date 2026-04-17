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
            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is null || product.Status != ProductStatus.Active || product.IsDeleted)
                return Result<FavoriteItemDto>.Failure("Product not found");

            var existing = await _favoriteRepository.GetByUserAndProductAsync(request.ActorUserId, product.Id, ct);
            if (existing is not null)
            {
                return Result<FavoriteItemDto>.Success(
                    new FavoriteItemDto(existing.Id.Value, existing.ProductId.Value, existing.AddedAt, existing.PriceAtAdd?.Amount, existing.IsAvailable));
            }

            var now = DateTime.UtcNow;
            var favorite = Favorite.Reconstitute(
                FavoriteId.From(0),
                request.ActorUserId,
                product.Id,
                now,
                product.Price,
                true,
                JsonBlob.Empty,
                null,
                now,
                now,
                false,
                null);

            favorite = await _favoriteRepository.AddAsync(favorite, ct);
            await _cache.RemoveAsync(FavoritesCacheKeys.ListByUser(request.ActorUserId), ct);
            return Result<FavoriteItemDto>.Success(
                new FavoriteItemDto(favorite.Id.Value, favorite.ProductId.Value, favorite.AddedAt, favorite.PriceAtAdd?.Amount, favorite.IsAvailable));
        }
        catch (Exception ex)
        {
            return Result<FavoriteItemDto>.Failure($"Failed to add favorite: {ex.Message}");
        }
    }
}
