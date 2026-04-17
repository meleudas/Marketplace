using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Favorites.Entities;

namespace Marketplace.Domain.Favorites.Repositories;

public interface IFavoriteRepository
{
    Task<Favorite?> GetByUserAndProductAsync(Guid userId, ProductId productId, CancellationToken ct = default);
    Task<IReadOnlyList<Favorite>> ListByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Favorite> AddAsync(Favorite favorite, CancellationToken ct = default);
    Task SoftDeleteAsync(FavoriteId id, DateTime utcNow, CancellationToken ct = default);
}
