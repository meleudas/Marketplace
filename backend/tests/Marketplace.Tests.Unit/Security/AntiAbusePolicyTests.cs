using Marketplace.Application.Common.Policies;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Options;
using Marketplace.Application.Notifications.Policies;
using Marketplace.Application.Payments.Options;
using Marketplace.Application.Payments.Policies;
using Marketplace.Application.Reviews.Options;
using Marketplace.Application.Reviews.Policies;
using Marketplace.Infrastructure.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Security")]
public sealed class AntiAbusePolicyTests
{
    [Fact]
    public async Task ReviewCreateAntiAbusePolicy_Rejects_Over_Limit()
    {
        var store = new MemoryRateLimitCounterStore(new MemoryCache(new MemoryCacheOptions()));
        var policy = new ReviewCreateAntiAbusePolicy(
            new CreateRateLimitPolicy(store),
            Options.Create(new ReviewsAntiAbuseOptions { CreatePerUserProductPerDay = 2, CreateWindowMinutes = 60 }));

        var userId = Guid.NewGuid();
        var first = await policy.EvaluateCreateAsync(userId, 42, CancellationToken.None);
        var second = await policy.EvaluateCreateAsync(userId, 42, CancellationToken.None);
        var third = await policy.EvaluateCreateAsync(userId, 42, CancellationToken.None);

        Assert.True(first.Allowed);
        Assert.True(second.Allowed);
        Assert.False(third.Allowed);
    }

    [Fact]
    public async Task PaymentWebhookAntiAbusePolicy_Rejects_Over_Limit_For_Ip()
    {
        var store = new MemoryRateLimitCounterStore(new MemoryCache(new MemoryCacheOptions()));
        var policy = new PaymentWebhookAntiAbusePolicy(
            new CreateRateLimitPolicy(store),
            Options.Create(new PaymentWebhookAntiAbuseOptions { FailedSignaturePerIpPerWindow = 2, FailedSignatureWindowMinutes = 5 }));

        await policy.RecordFailedSignatureAsync("203.0.113.10", CancellationToken.None);
        await policy.RecordFailedSignatureAsync("203.0.113.10", CancellationToken.None);
        var blocked = await policy.EvaluateClientIpAsync("203.0.113.10", CancellationToken.None);

        Assert.False(blocked.Allowed);
    }

    [Fact]
    public async Task NotificationDispatchAntiAbusePolicy_Rejects_Burst_Over_Limit()
    {
        var store = new MemoryRateLimitCounterStore(new MemoryCache(new MemoryCacheOptions()));
        var policy = new NotificationDispatchAntiAbusePolicy(
            new CreateRateLimitPolicy(store),
            Options.Create(new NotificationDispatchAntiAbuseOptions { BurstPerTargetPerMinute = 2, BurstWindowSeconds = 60 }));

        var request = new AppNotificationRequest { TemplateKey = "UserOrderStatus", TargetUserId = Guid.NewGuid() };
        Assert.True((await policy.EvaluateScheduleAsync(request)).Allowed);
        Assert.True((await policy.EvaluateScheduleAsync(request)).Allowed);
        Assert.False((await policy.EvaluateScheduleAsync(request)).Allowed);
    }
}

[Trait("Suite", "Reviews")]
public sealed class ReviewAntiAbusePolicyTests
{
    [Fact]
    public async Task ReviewCreateAntiAbusePolicy_Allows_Different_Products_For_Same_User()
    {
        var policy = new ReviewCreateAntiAbusePolicy(
            new CreateRateLimitPolicy(new MemoryRateLimitCounterStore(new MemoryCache(new MemoryCacheOptions()))),
            Options.Create(new ReviewsAntiAbuseOptions { CreatePerUserProductPerDay = 1 }));

        var userId = Guid.NewGuid();
        var productA = await policy.EvaluateCreateAsync(userId, 1, CancellationToken.None);
        var productB = await policy.EvaluateCreateAsync(userId, 2, CancellationToken.None);

        Assert.True(productA.Allowed);
        Assert.True(productB.Allowed);
    }
}
