using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Cache;

namespace Marketplace.Tests;

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
