using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Marketplace.Infrastructure.Observability;

namespace Marketplace.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CacheLatencyMs,
            new KeyValuePair<string, object?>("backend", "redis"),
            new KeyValuePair<string, object?>("op", "get"));
        try
        {
            var json = await _cache.GetStringAsync(key, ct);
            if (json is null) MarketplaceMetrics.CacheMisses.Add(1, [new KeyValuePair<string, object?>("backend", "redis")]);
            else MarketplaceMetrics.CacheHits.Add(1, [new KeyValuePair<string, object?>("backend", "redis")]);
            return json is null ? null : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            MarketplaceMetrics.CacheErrors.Add(1, [new KeyValuePair<string, object?>("backend", "redis"), new KeyValuePair<string, object?>("op", "get")]);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CacheLatencyMs,
            new KeyValuePair<string, object?>("backend", "redis"),
            new KeyValuePair<string, object?>("op", "set"));
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();
        if (ttl is { } t)
            options.AbsoluteExpirationRelativeToNow = t;
        try
        {
            await _cache.SetStringAsync(key, json, options, ct);
        }
        catch
        {
            MarketplaceMetrics.CacheErrors.Add(1, [new KeyValuePair<string, object?>("backend", "redis"), new KeyValuePair<string, object?>("op", "set")]);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await RemoveCoreAsync(key, ct);

    private async Task RemoveCoreAsync(string key, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CacheLatencyMs,
            new KeyValuePair<string, object?>("backend", "redis"),
            new KeyValuePair<string, object?>("op", "remove"));
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch
        {
            MarketplaceMetrics.CacheErrors.Add(1, [new KeyValuePair<string, object?>("backend", "redis"), new KeyValuePair<string, object?>("op", "remove")]);
            throw;
        }
    }
}
