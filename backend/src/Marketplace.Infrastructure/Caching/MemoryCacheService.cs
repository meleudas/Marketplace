using Microsoft.Extensions.Caching.Memory;
using Marketplace.Infrastructure.Observability;

namespace Marketplace.Infrastructure.Caching;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CacheLatencyMs,
            new KeyValuePair<string, object?>("backend", "memory"),
            new KeyValuePair<string, object?>("op", "get"));
        try
        {
            _cache.TryGetValue(key, out T? value);
            if (value is null) MarketplaceMetrics.CacheMisses.Add(1, [new KeyValuePair<string, object?>("backend", "memory")]);
            else MarketplaceMetrics.CacheHits.Add(1, [new KeyValuePair<string, object?>("backend", "memory")]);
            return Task.FromResult(value);
        }
        catch
        {
            MarketplaceMetrics.CacheErrors.Add(1, [new KeyValuePair<string, object?>("backend", "memory"), new KeyValuePair<string, object?>("op", "get")]);
            throw;
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CacheLatencyMs,
            new KeyValuePair<string, object?>("backend", "memory"),
            new KeyValuePair<string, object?>("op", "set"));
        var options = new MemoryCacheEntryOptions();
        if (ttl is { } t)
            options.AbsoluteExpirationRelativeToNow = t;
        try
        {
            _cache.Set(key, value, options);
        }
        catch
        {
            MarketplaceMetrics.CacheErrors.Add(1, [new KeyValuePair<string, object?>("backend", "memory"), new KeyValuePair<string, object?>("op", "set")]);
            throw;
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CacheLatencyMs,
            new KeyValuePair<string, object?>("backend", "memory"),
            new KeyValuePair<string, object?>("op", "remove"));
        try
        {
            _cache.Remove(key);
        }
        catch
        {
            MarketplaceMetrics.CacheErrors.Add(1, [new KeyValuePair<string, object?>("backend", "memory"), new KeyValuePair<string, object?>("op", "remove")]);
            throw;
        }
        return Task.CompletedTask;
    }
}
