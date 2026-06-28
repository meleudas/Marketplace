namespace Marketplace.Application.Carts.Ports;

public interface ICartStockWatchRepository
{
    Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default);
    Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns user ids with a watch on this product eligible for a new notify (rate limit).</summary>
    Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(
        long productId,
        TimeSpan minIntervalSinceLastNotify,
        DateTime utcNow,
        CancellationToken ct = default);

    Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default);
}
