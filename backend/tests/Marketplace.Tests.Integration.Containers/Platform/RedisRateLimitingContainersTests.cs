using Marketplace.Application.Common.RateLimiting;
using Marketplace.Infrastructure.RateLimiting;
using Marketplace.Tests.Fixtures;
using StackExchange.Redis;
using Xunit;

namespace Marketplace.Tests.Platform;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Platform")]
[Trait("Layer", "IntegrationContainers")]
public sealed class RedisRateLimitingContainersTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public RedisRateLimitingContainersTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RedisRateLimitCounterStore_Enforces_Window_Limit()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var store = new RedisRateLimitCounterStore(redis);
        var key = $"test:{Guid.NewGuid():N}";
        var window = TimeSpan.FromSeconds(30);

        var first = await store.TryAcquireAsync(key, window, 2, CancellationToken.None);
        var second = await store.TryAcquireAsync(key, window, 2, CancellationToken.None);
        var third = await store.TryAcquireAsync(key, window, 2, CancellationToken.None);

        Assert.True(first.Allowed);
        Assert.True(second.Allowed);
        Assert.False(third.Allowed);
        Assert.True(third.RetryAfterSeconds > 0);
    }
}
