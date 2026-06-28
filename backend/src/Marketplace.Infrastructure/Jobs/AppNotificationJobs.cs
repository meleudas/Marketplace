using Hangfire;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Notifications;
using Marketplace.Application.Common.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Marketplace.Infrastructure.Jobs;

public sealed class AppNotificationJobs
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly AppNotificationPayloadBuilder _payloadBuilder;
    private readonly IInAppNotificationRepository _inAppNotifications;
    private readonly IOptionsMonitor<AppNotificationOptions> _appNotificationOptions;
    private readonly IIntegrationRetryStore _integrationRetryStore;
    private readonly IntegrationRetryOptions _retryOptions;
    private readonly ILogger<AppNotificationJobs> _logger;

    public AppNotificationJobs(
        IEnumerable<INotificationChannel> channels,
        AppNotificationPayloadBuilder payloadBuilder,
        IInAppNotificationRepository inAppNotifications,
        IOptionsMonitor<AppNotificationOptions> appNotificationOptions,
        IIntegrationRetryStore integrationRetryStore,
        IOptions<IntegrationRetryOptions> retryOptions,
        ILogger<AppNotificationJobs> logger)
    {
        _channels = channels;
        _payloadBuilder = payloadBuilder;
        _inAppNotifications = inAppNotifications;
        _appNotificationOptions = appNotificationOptions;
        _integrationRetryStore = integrationRetryStore;
        _retryOptions = retryOptions.Value;
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
        using var dispatchTimer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.NotificationDispatchLatencyMs,
            new KeyValuePair<string, object?>("template_key", templateKey));
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "app-notification-dispatch"),
            new KeyValuePair<string, object?>("template_key", templateKey));

        try
        {
            MarketplaceMetrics.NotificationDispatches.Add(1,
            [
                new KeyValuePair<string, object?>("template_key", templateKey),
                new KeyValuePair<string, object?>("audience", ((AppNotificationAudienceKind)audience).ToString())
            ]);

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

                try
                {
                    await channel.DeliverAsync(envelope, ct);
                    MarketplaceMetrics.NotificationChannelDeliveries.Add(1,
                    [
                        new KeyValuePair<string, object?>("template_key", templateKey),
                        new KeyValuePair<string, object?>("channel", channel.Kind.ToString()),
                        new KeyValuePair<string, object?>("status", "success")
                    ]);
                }
                catch (Exception ex)
                {
                    MarketplaceMetrics.NotificationChannelErrors.Add(1,
                    [
                        new KeyValuePair<string, object?>("template_key", templateKey),
                        new KeyValuePair<string, object?>("channel", channel.Kind.ToString())
                    ]);
                    MarketplaceMetrics.NotificationChannelDeliveries.Add(1,
                    [
                        new KeyValuePair<string, object?>("template_key", templateKey),
                        new KeyValuePair<string, object?>("channel", channel.Kind.ToString()),
                        new KeyValuePair<string, object?>("status", "failed")
                    ]);
                    var nextAttempt = RetryBackoffCalculator.ComputeNextAttemptUtc(
                        1,
                        _retryOptions.BaseBackoffMinutes,
                        _retryOptions.MaxBackoffMinutes,
                        DateTime.UtcNow);
                    await _integrationRetryStore.UpsertAsync(
                        new IntegrationRetryUpsert(
                            IntegrationRetryKinds.NotificationDispatch,
                            "AppNotification",
                            $"{correlationId:N}:{channel.Kind}",
                            JsonSerializer.Serialize(new
                            {
                                templateKey,
                                correlationId,
                                channels,
                                audience,
                                targetUserId,
                                targetCompanyId,
                                payloadJson,
                                channel = channel.Kind.ToString()
                            }),
                            ex.Message),
                        nextAttempt,
                        ct);
                    throw;
                }
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
            MarketplaceMetrics.NotificationDispatchErrors.Add(1,
            [
                new KeyValuePair<string, object?>("template_key", templateKey)
            ]);
            MarketplaceMetrics.HangfireJobErrors.Add(1,
            [
                new KeyValuePair<string, object?>("job", "app-notification-dispatch"),
                new KeyValuePair<string, object?>("template_key", templateKey)
            ]);
            _logger.LogError(ex, "App notification dispatch failed for {Template}", templateKey);
            throw;
        }
    }

    public Task PruneExpiredInAppNotificationsAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("app-notifications-prune-expired-inapp", PruneExpiredInAppNotificationsCoreAsync, ct);

    private async Task PruneExpiredInAppNotificationsCoreAsync(CancellationToken ct)
    {
        if (!_appNotificationOptions.CurrentValue.PruneExpiredInAppEnabled)
            return;

        var removed = await _inAppNotifications.DeleteExpiredBeforeAsync(DateTime.UtcNow, ct);
        if (removed > 0)
            _logger.LogInformation("Removed {Count} expired in-app notifications.", removed);
    }
}
