using System.Diagnostics;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;

namespace Marketplace.Tests;

public sealed class PerformanceBaselineTests
{
    [Fact]
    [Trait("Suite", "Performance")]
    public async Task Orders_Cache_Invalidation_Baseline_Is_Within_Threshold()
    {
        var cache = new InMemoryAppCache();
        var service = new OrderCacheInvalidationService(cache);
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100; i++)
            await service.InvalidateOrderAsync(i + 1, userId, companyId, CancellationToken.None);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Cache invalidation baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Suite", "Performance")]
    public async Task Orders_List_Version_Read_Baseline_Is_Within_Threshold()
    {
        var cache = new InMemoryAppCache();
        var service = new OrderCacheInvalidationService(cache);
        var userId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
            _ = await service.GetListVersionAsync("my", userId, null, CancellationToken.None);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1500, $"List version baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    private sealed class InMemoryAppCache : IAppCachePort
    {
        private readonly Dictionary<string, object> _items = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_items.TryGetValue(key, out var value) ? value as T : null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            _items[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _items.Remove(key);
            return Task.CompletedTask;
        }
    }
}
