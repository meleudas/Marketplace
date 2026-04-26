using Marketplace.Application.Common.Ports;

namespace Marketplace.Application.Orders.Cache;

public sealed class OrderCacheInvalidationService : IOrderCacheInvalidationService
{
    private sealed record CacheVersionBox(long Value);

    private readonly IAppCachePort _cache;
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(30);

    public OrderCacheInvalidationService(IAppCachePort cache)
    {
        _cache = cache;
    }

    public async Task<long> GetListVersionAsync(string scope, Guid? actorUserId, Guid? companyId, CancellationToken ct = default)
    {
        var key = ResolveVersionKey(scope, actorUserId, companyId);
        if (key is null)
            return 1;

        var box = await _cache.GetAsync<CacheVersionBox>(key, ct);
        if (box is not null && box.Value > 0)
            return box.Value;

        await _cache.SetAsync(key, new CacheVersionBox(1), VersionTtl, ct);
        return 1;
    }

    public async Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(OrderCacheKeys.Detail(orderId), ct);
        await BumpVersionAsync(OrderCacheKeys.AdminListVersion(), ct);
        await BumpVersionAsync(OrderCacheKeys.CompanyListVersion(companyId), ct);
        await BumpVersionAsync(OrderCacheKeys.MyListVersion(customerId), ct);
    }

    private async Task BumpVersionAsync(string key, CancellationToken ct)
    {
        var current = await _cache.GetAsync<CacheVersionBox>(key, ct);
        var next = Math.Max(1, current?.Value ?? 1) + 1;
        await _cache.SetAsync(key, new CacheVersionBox(next), VersionTtl, ct);
    }

    private static string? ResolveVersionKey(string scope, Guid? actorUserId, Guid? companyId)
    {
        return scope.Trim().ToLowerInvariant() switch
        {
            "admin" => OrderCacheKeys.AdminListVersion(),
            "company" when companyId.HasValue => OrderCacheKeys.CompanyListVersion(companyId.Value),
            "my" when actorUserId.HasValue => OrderCacheKeys.MyListVersion(actorUserId.Value),
            _ => null
        };
    }
}
