using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public sealed class ApplicationOrderCacheInvalidationServiceTests
{
    [Fact]
    public async Task GetListVersion_Defaults_To_One_And_Bumps_On_Invalidate()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var cache = new InMemoryAppCache();
        var sut = new OrderCacheInvalidationService(cache);

        var before = await sut.GetListVersionAsync("my", userId, null, CancellationToken.None);
        await sut.InvalidateOrderAsync(10, userId, companyId, CancellationToken.None);
        var after = await sut.GetListVersionAsync("my", userId, null, CancellationToken.None);

        Assert.Equal(1, before);
        Assert.True(after > before);
    }

    [Fact]
    public async Task InvalidateOrder_Removes_Tracked_Detail_And_List_Keys()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var cache = new InMemoryAppCache();
        var sut = new OrderCacheInvalidationService(cache);
        const long orderId = 42;

        var detailKey = OrderCacheKeys.Detail(orderId);
        var listKeyAdmin = OrderCacheKeys.List(1, "admin", null, null, null, null, null, null, null, 1, 20);
        var listKeyCompany = OrderCacheKeys.List(1, "company", null, companyId, null, null, null, null, null, 1, 20);
        var listKeyMy = OrderCacheKeys.List(1, "my", userId, null, null, null, null, null, null, 1, 20);

        await cache.SetAsync(detailKey, new Box("detail"), TimeSpan.FromMinutes(5), CancellationToken.None);
        await cache.SetAsync(listKeyAdmin, new Box("admin"), TimeSpan.FromMinutes(5), CancellationToken.None);
        await cache.SetAsync(listKeyCompany, new Box("company"), TimeSpan.FromMinutes(5), CancellationToken.None);
        await cache.SetAsync(listKeyMy, new Box("my"), TimeSpan.FromMinutes(5), CancellationToken.None);

        await sut.TrackDetailKeyAsync(orderId, detailKey, TimeSpan.FromMinutes(5), CancellationToken.None);
        await sut.TrackListKeyAsync("admin", null, null, listKeyAdmin, TimeSpan.FromMinutes(5), CancellationToken.None);
        await sut.TrackListKeyAsync("company", null, companyId, listKeyCompany, TimeSpan.FromMinutes(5), CancellationToken.None);
        await sut.TrackListKeyAsync("my", userId, null, listKeyMy, TimeSpan.FromMinutes(5), CancellationToken.None);

        await sut.InvalidateOrderAsync(orderId, userId, companyId, CancellationToken.None);

        Assert.False(cache.Contains(detailKey));
        Assert.False(cache.Contains(listKeyAdmin));
        Assert.False(cache.Contains(listKeyCompany));
        Assert.False(cache.Contains(listKeyMy));
    }

    [Fact]
    public async Task InvalidateOrder_Is_Idempotent()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var cache = new InMemoryAppCache();
        var sut = new OrderCacheInvalidationService(cache);
        const long orderId = 99;

        await sut.TrackDetailKeyAsync(orderId, OrderCacheKeys.Detail(orderId), TimeSpan.FromMinutes(5), CancellationToken.None);

        await sut.InvalidateOrderAsync(orderId, userId, companyId, CancellationToken.None);
        await sut.InvalidateOrderAsync(orderId, userId, companyId, CancellationToken.None);

        var version = await sut.GetListVersionAsync("my", userId, null, CancellationToken.None);
        Assert.True(version >= 3);
    }

    [Fact]
    public async Task Service_Is_FailSoft_When_Cache_Throws()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var cache = new ThrowingAppCache();
        var sut = new OrderCacheInvalidationService(cache);

        var ex = await Record.ExceptionAsync(async () =>
        {
            await sut.TrackDetailKeyAsync(7, OrderCacheKeys.Detail(7), TimeSpan.FromMinutes(1), CancellationToken.None);
            await sut.TrackListKeyAsync("admin", null, null, "orders:list:test", TimeSpan.FromMinutes(1), CancellationToken.None);
            await sut.InvalidateOrderAsync(7, userId, companyId, CancellationToken.None);
            _ = await sut.GetListVersionAsync("admin", null, null, CancellationToken.None);
        });

        Assert.Null(ex);
    }

    private sealed record Box(string Value);

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

        public bool Contains(string key) => _items.ContainsKey(key);
    }

    private sealed class ThrowingAppCache : IAppCachePort
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => throw new InvalidOperationException("Cache unavailable");

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
            => throw new InvalidOperationException("Cache unavailable");

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => throw new InvalidOperationException("Cache unavailable");
    }
}
