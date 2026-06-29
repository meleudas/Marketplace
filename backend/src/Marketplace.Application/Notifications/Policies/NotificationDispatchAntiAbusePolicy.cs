using Marketplace.Application.Common.Policies;
using Marketplace.Application.Notifications.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Notifications.Policies;

public sealed class NotificationDispatchAntiAbusePolicy
{
    private readonly CreateRateLimitPolicy _rateLimit;
    private readonly NotificationDispatchAntiAbuseOptions _options;

    public NotificationDispatchAntiAbusePolicy(CreateRateLimitPolicy rateLimit, IOptions<NotificationDispatchAntiAbuseOptions> options)
    {
        _rateLimit = rateLimit;
        _options = options.Value;
    }

    public Task<(bool Allowed, string? Reason, int RetryAfterSeconds)> EvaluateScheduleAsync(
        AppNotificationRequest request,
        CancellationToken ct = default)
    {
        var window = TimeSpan.FromSeconds(Math.Max(1, _options.BurstWindowSeconds));
        var target = request.TargetUserId?.ToString("N")
            ?? request.TargetCompanyId?.ToString("N")
            ?? request.TemplateKey;
        var key = $"notification-dispatch:{request.TemplateKey}:{target}";
        return _rateLimit.EvaluateAsync(key, _options.BurstPerTargetPerMinute, window, ct);
    }
}
