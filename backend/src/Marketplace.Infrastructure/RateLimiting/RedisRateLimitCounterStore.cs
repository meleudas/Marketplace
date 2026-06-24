using Marketplace.Application.Common.RateLimiting;
using StackExchange.Redis;

namespace Marketplace.Infrastructure.RateLimiting;

public sealed class RedisRateLimitCounterStore : IRateLimitCounterStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimitCounterStore(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<RateLimitAcquireResult> TryAcquireAsync(string key, TimeSpan window, int permitLimit, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";
        var count = await db.StringIncrementAsync(redisKey);
        if (count == 1)
            await db.KeyExpireAsync(redisKey, window);

        if (count > permitLimit)
        {
            var ttl = await db.KeyTimeToLiveAsync(redisKey);
            var retryAfter = ttl.HasValue ? (int)Math.Ceiling(ttl.Value.TotalSeconds) : (int)window.TotalSeconds;
            return new RateLimitAcquireResult(false, Math.Max(1, retryAfter));
        }

        return new RateLimitAcquireResult(true, 0);
    }
}
