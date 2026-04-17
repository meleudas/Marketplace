using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Favorites.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;

public sealed class RemoveFavoriteProductCommandHandler : IRequestHandler<RemoveFavoriteProductCommand, Result<bool>>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IAppCachePort _cache;

    public RemoveFavoriteProductCommandHandler(IFavoriteRepository favoriteRepository, IAppCachePort cache)
    {
        _favoriteRepository = favoriteRepository;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(RemoveFavoriteProductCommand request, CancellationToken ct)
    {
        try
        {
            var favorite = await _favoriteRepository.GetByUserAndProductAsync(request.ActorUserId, ProductId.From(request.ProductId), ct);
            if (favorite is null)
                return Result<bool>.Success(true);

            await _favoriteRepository.SoftDeleteAsync(favorite.Id, DateTime.UtcNow, ct);
            await _cache.RemoveAsync(FavoritesCacheKeys.ListByUser(request.ActorUserId), ct);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to remove favorite: {ex.Message}");
        }
    }
}
