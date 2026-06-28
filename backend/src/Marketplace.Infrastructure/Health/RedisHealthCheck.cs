using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace Marketplace.Infrastructure.Health;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache) => _cache = cache;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var key = "health:ping";
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes("ok"), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            }, ct);
            var value = await _cache.GetAsync(key, ct);
            return value is { Length: > 0 }
                ? HealthCheckResult.Healthy("Redis cache is reachable")
                : HealthCheckResult.Degraded("Redis cache read failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Redis check failed", ex);
        }
    }
}
