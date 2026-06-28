namespace Marketplace.Application.Orders.Cache;

public interface IOrderCacheInvalidationService
{
    Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default);
    Task TrackDetailKeyAsync(long orderId, string cacheKey, TimeSpan ttl, CancellationToken ct = default);
    Task TrackListKeyAsync(string scope, Guid? actorUserId, Guid? companyId, string cacheKey, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default);
}
