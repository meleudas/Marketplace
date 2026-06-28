using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.Cache;
using Marketplace.Application.Reviews.Cache;
using Marketplace.Application.Notifications;
using Marketplace.Application.Orders.Cache;

namespace Marketplace.Tests;

public sealed class PerformanceBaselineTests
{
    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "Orders")]
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
    [Trait("Suite", "Orders")]
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

    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "Favorites")]
    public async Task Favorites_Cache_Invalidation_Baseline_Is_Within_Threshold()
    {
        var cache = new InMemoryAppCache();
        var userId = Guid.NewGuid();
        var key = FavoritesCacheKeys.ListByUser(userId);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100; i++)
        {
            await cache.SetAsync(key, new List<int> { i }, TimeSpan.FromMinutes(1), CancellationToken.None);
            await cache.RemoveAsync(key, CancellationToken.None);
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1500, $"Favorites cache invalidation baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "Favorites")]
    public async Task Favorites_List_Read_Baseline_Is_Within_Threshold()
    {
        var cache = new InMemoryAppCache();
        var userId = Guid.NewGuid();
        var key = FavoritesCacheKeys.ListByUser(userId);
        await cache.SetAsync(key, new List<int> { 1, 2, 3 }, TimeSpan.FromMinutes(1), CancellationToken.None);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
            _ = await cache.GetAsync<List<int>>(key, CancellationToken.None);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1200, $"Favorites list baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "IdentityAccess")]
    public void Identity_RefreshTokenHash_Baseline_Is_Within_Threshold()
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 5000; i++)
        {
            _ = SHA256.HashData(Encoding.UTF8.GetBytes(token + i.ToString()));
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1500, $"Refresh token hash baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "Payments")]
    public void Payments_Webhook_DedupHash_Baseline_Is_Within_Threshold()
    {
        const string payload = "tx-1|success|sig|data";
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 5000; i++)
            _ = SHA256.HashData(Encoding.UTF8.GetBytes(payload + i));

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1500, $"Payments dedup hash baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "ProductsModeration")]
    public void Products_Moderation_CorrelationId_Baseline_Is_Within_Threshold()
    {
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 5000; i++)
            _ = AppNotificationCorrelationIds.ProductPendingReviewQueue(i + 1);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1500, $"Products moderation correlation baseline degraded: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Suite", "Performance")]
    [Trait("Suite", "Reviews")]
    public async Task Reviews_Cache_Key_Read_Baseline_Is_Within_Threshold()
    {
        var cache = new InMemoryAppCache();
        var key = ReviewCacheKeys.ProductList(10, 1, 20);
        await cache.SetAsync(key, new List<int> { 1, 2 }, TimeSpan.FromMinutes(1), CancellationToken.None);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
            _ = await cache.GetAsync<List<int>>(key, CancellationToken.None);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1200, $"Reviews cache key baseline degraded: {sw.ElapsedMilliseconds}ms");
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
