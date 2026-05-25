using Hangfire;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Notifications;
using Marketplace.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class AppNotificationJobs
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly AppNotificationPayloadBuilder _payloadBuilder;
    private readonly IInAppNotificationRepository _inAppNotifications;
    private readonly IOptionsMonitor<AppNotificationOptions> _appNotificationOptions;
    private readonly ILogger<AppNotificationJobs> _logger;

    public AppNotificationJobs(
        IEnumerable<INotificationChannel> channels,
        AppNotificationPayloadBuilder payloadBuilder,
        IInAppNotificationRepository inAppNotifications,
        IOptionsMonitor<AppNotificationOptions> appNotificationOptions,
        ILogger<AppNotificationJobs> logger)
    {
        _channels = channels;
        _payloadBuilder = payloadBuilder;
        _inAppNotifications = inAppNotifications;
        _appNotificationOptions = appNotificationOptions;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task DispatchAsync(
        string templateKey,
        Guid correlationId,
        int channels,
        int audience,
        Guid? targetUserId,
        Guid? targetCompanyId,
        string? payloadJson,
        CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "app-notification-dispatch"),
            new KeyValuePair<string, object?>("template_key", templateKey));

        try
        {
            var request = new AppNotificationRequest
            {
                TemplateKey = templateKey,
                CorrelationId = correlationId,
                Channels = (AppNotificationChannelKind)channels,
                Audience = (AppNotificationAudienceKind)audience,
                TargetUserId = targetUserId,
                TargetCompanyId = targetCompanyId,
                PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson
            };

            var envelope = _payloadBuilder.Build(request);
            foreach (var channel in _channels)
            {
                if ((envelope.Channels & channel.Kind) != channel.Kind)
                    continue;

                await channel.DeliverAsync(envelope, ct);
            }

            MarketplaceMetrics.HangfireJobs.Add(1,
            [
                new KeyValuePair<string, object?>("job", "app-notification-dispatch"),
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("template_key", templateKey)
            ]);
        }
        catch (Exception ex)
        {
            MarketplaceMetrics.HangfireJobErrors.Add(1,
            [
                new KeyValuePair<string, object?>("job", "app-notification-dispatch"),
                new KeyValuePair<string, object?>("template_key", templateKey)
            ]);
            _logger.LogError(ex, "App notification dispatch failed for {Template}", templateKey);
            throw;
        }
    }

    public async Task PruneExpiredInAppNotificationsAsync(CancellationToken ct = default)
    {
        if (!_appNotificationOptions.CurrentValue.PruneExpiredInAppEnabled)
            return;

        var removed = await _inAppNotifications.DeleteExpiredBeforeAsync(DateTime.UtcNow, ct);
        if (removed > 0)
            _logger.LogInformation("Removed {Count} expired in-app notifications.", removed);
    }
}
