using Marketplace.Application.Common.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace Marketplace.Infrastructure.RateLimiting;

public sealed class MemoryRateLimitCounterStore : IRateLimitCounterStore
{
    private readonly IMemoryCache _cache;

    public MemoryRateLimitCounterStore(IMemoryCache cache) => _cache = cache;

    public Task<RateLimitAcquireResult> TryAcquireAsync(string key, TimeSpan window, int permitLimit, CancellationToken ct = default)
    {
        var cacheKey = $"ratelimit:{key}";
        var now = DateTimeOffset.UtcNow;
        var entry = _cache.GetOrCreate(cacheKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = window;
            return new CounterWindow(now, 0);
        })!;

        if (now - entry.WindowStart > window)
        {
            entry = new CounterWindow(now, 0);
            _cache.Set(cacheKey, entry, now.Add(window));
        }

        if (entry.Count >= permitLimit)
        {
            var retryAfter = (int)Math.Ceiling((entry.WindowStart.Add(window) - now).TotalSeconds);
            return Task.FromResult(new RateLimitAcquireResult(false, Math.Max(1, retryAfter)));
        }

        entry.Count++;
        _cache.Set(cacheKey, entry, entry.WindowStart.Add(window));
        return Task.FromResult(new RateLimitAcquireResult(true, 0));
    }

    private sealed class CounterWindow
    {
        public CounterWindow(DateTimeOffset windowStart, int count)
        {
            WindowStart = windowStart;
            Count = count;
        }

        public DateTimeOffset WindowStart { get; set; }
        public int Count { get; set; }
    }
}
