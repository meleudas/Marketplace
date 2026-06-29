using Marketplace.Application.Common.Policies;
using Marketplace.Application.Notifications.Options;
using Marketplace.Application.Notifications.Policies;
using Marketplace.Application.Payments.Options;
using Marketplace.Application.Payments.Policies;
using Marketplace.Application.Reviews.Options;
using Marketplace.Application.Reviews.Policies;
using Marketplace.Infrastructure.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests.Common.Fakes;

public static class AntiAbuseTestDoubles
{
    private static CreateRateLimitPolicy CreatePermissiveRateLimit() =>
        new(new MemoryRateLimitCounterStore(new MemoryCache(new MemoryCacheOptions())));

    public static ReviewCreateAntiAbusePolicy PermissiveReviewCreate() =>
        new(CreatePermissiveRateLimit(),
            Options.Create(new ReviewsAntiAbuseOptions { CreatePerUserProductPerDay = 10_000 }));

    public static PaymentWebhookAntiAbusePolicy PermissivePaymentWebhook() =>
        new(CreatePermissiveRateLimit(),
            Options.Create(new PaymentWebhookAntiAbuseOptions { FailedSignaturePerIpPerWindow = 10_000 }));

    public static NotificationDispatchAntiAbusePolicy PermissiveNotificationDispatch() =>
        new(CreatePermissiveRateLimit(),
            Options.Create(new NotificationDispatchAntiAbuseOptions { BurstPerTargetPerMinute = 10_000 }));
}
