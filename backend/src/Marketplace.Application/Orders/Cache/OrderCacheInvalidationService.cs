using Marketplace.Application.Common.Ports;

namespace Marketplace.Application.Orders.Cache;

public sealed class OrderCacheInvalidationService : IOrderCacheInvalidationService
{
    private sealed record CacheVersionBox(long Value);
    private sealed record CacheKeySetBox(IReadOnlyList<string> Keys);

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

        var box = await TryGetAsync<CacheVersionBox>(key, ct);
        if (box is not null && box.Value > 0)
            return box.Value;

        await TrySetAsync(key, new CacheVersionBox(1), VersionTtl, ct);
        return 1;
    }

    public Task TrackDetailKeyAsync(long orderId, string cacheKey, TimeSpan ttl, CancellationToken ct = default)
        => TrackKeyAsync(OrderCacheKeys.DetailIndex(orderId), cacheKey, ttl, ct);

    public Task TrackListKeyAsync(string scope, Guid? actorUserId, Guid? companyId, string cacheKey, TimeSpan ttl, CancellationToken ct = default)
        => TrackKeyAsync(OrderCacheKeys.ListIndex(scope, actorUserId, companyId), cacheKey, ttl, ct);

    public async Task InvalidateOrderAsync(long orderId, Guid customerId, Guid companyId, CancellationToken ct = default)
    {
        await TryRemoveAsync(OrderCacheKeys.Detail(orderId), ct);
        await RemoveTrackedKeysAsync(OrderCacheKeys.DetailIndex(orderId), ct);
        await RemoveTrackedKeysAsync(OrderCacheKeys.ListIndex("admin", null, null), ct);
        await RemoveTrackedKeysAsync(OrderCacheKeys.ListIndex("company", null, companyId), ct);
        await RemoveTrackedKeysAsync(OrderCacheKeys.ListIndex("my", customerId, null), ct);
        await BumpVersionAsync(OrderCacheKeys.AdminListVersion(), ct);
        await BumpVersionAsync(OrderCacheKeys.CompanyListVersion(companyId), ct);
        await BumpVersionAsync(OrderCacheKeys.MyListVersion(customerId), ct);
    }

    private async Task BumpVersionAsync(string key, CancellationToken ct)
    {
        var current = await TryGetAsync<CacheVersionBox>(key, ct);
        var next = Math.Max(1, current?.Value ?? 1) + 1;
        await TrySetAsync(key, new CacheVersionBox(next), VersionTtl, ct);
    }

    private async Task TrackKeyAsync(string indexKey, string payloadKey, TimeSpan payloadTtl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payloadKey))
            return;

        var current = await TryGetAsync<CacheKeySetBox>(indexKey, ct);
        var merged = (current?.Keys ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Append(payloadKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var indexTtl = payloadTtl > VersionTtl ? payloadTtl : VersionTtl;
        await TrySetAsync(indexKey, new CacheKeySetBox(merged), indexTtl, ct);
    }

    private async Task RemoveTrackedKeysAsync(string indexKey, CancellationToken ct)
    {
        var tracked = await TryGetAsync<CacheKeySetBox>(indexKey, ct);
        if (tracked is not null)
        {
            foreach (var key in tracked.Keys.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
                await TryRemoveAsync(key, ct);
        }

        await TryRemoveAsync(indexKey, ct);
    }

    private async Task<T?> TryGetAsync<T>(string key, CancellationToken ct) where T : class
    {
        try
        {
            return await _cache.GetAsync<T>(key, ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task TrySetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class
    {
        try
        {
            await _cache.SetAsync(key, value, ttl, ct);
        }
        catch
        {
        }
    }

    private async Task TryRemoveAsync(string key, CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch
        {
        }
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
