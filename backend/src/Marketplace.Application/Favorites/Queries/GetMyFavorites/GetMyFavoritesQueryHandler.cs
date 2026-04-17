using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Application.Favorites.DTOs;
using Marketplace.Domain.Favorites.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Favorites.Queries.GetMyFavorites;

public sealed class GetMyFavoritesQueryHandler : IRequestHandler<GetMyFavoritesQuery, Result<IReadOnlyList<FavoriteItemDto>>>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetMyFavoritesQueryHandler(IFavoriteRepository favoriteRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _favoriteRepository = favoriteRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<IReadOnlyList<FavoriteItemDto>>> Handle(GetMyFavoritesQuery request, CancellationToken ct)
    {
        try
        {
            var cacheKey = FavoritesCacheKeys.ListByUser(request.ActorUserId);
            var cached = await _cache.GetAsync<List<FavoriteItemDto>>(cacheKey, ct);
            if (cached is not null)
                return Result<IReadOnlyList<FavoriteItemDto>>.Success(cached);

            var favorites = await _favoriteRepository.ListByUserIdAsync(request.ActorUserId, ct);
            var dto = favorites
                .Select(x => new FavoriteItemDto(x.Id.Value, x.ProductId.Value, x.AddedAt, x.PriceAtAdd?.Amount, x.IsAvailable))
                .ToList();
            await _cache.SetAsync(cacheKey, dto, _ttl.Favorites, ct);
            return Result<IReadOnlyList<FavoriteItemDto>>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<FavoriteItemDto>>.Failure($"Failed to get favorites: {ex.Message}");
        }
    }
}
