using Marketplace.Application.Common.Ports;

namespace Marketplace.Infrastructure.Caching;

public sealed class AppCachePort : IAppCachePort
{
    private readonly ICacheService _cacheService;

    public AppCachePort(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => _cacheService.GetAsync<T>(key, ct);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        => _cacheService.SetAsync(key, value, ttl, ct);

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cacheService.RemoveAsync(key, ct);
}
