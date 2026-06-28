using System.Net;
using System.Text.Json;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class WebPushNotificationChannel : INotificationChannel
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IPushDeliveryClient _delivery;
    private readonly IOptionsMonitor<WebPushOptions> _options;
    private readonly IAdminNotificationRecipientIds _adminRecipients;
    private readonly ICompanyOrderNotificationRecipientIds _companyRecipients;
    private readonly ILogger<WebPushNotificationChannel> _logger;

    public WebPushNotificationChannel(
        IPushSubscriptionRepository subscriptions,
        IPushDeliveryClient delivery,
        IOptionsMonitor<WebPushOptions> options,
        IAdminNotificationRecipientIds adminRecipients,
        ICompanyOrderNotificationRecipientIds companyRecipients,
        ILogger<WebPushNotificationChannel> logger)
    {
        _subscriptions = subscriptions;
        _delivery = delivery;
        _options = options;
        _adminRecipients = adminRecipients;
        _companyRecipients = companyRecipients;
        _logger = logger;
    }

    public AppNotificationChannelKind Kind => AppNotificationChannelKind.Push;

    public async Task DeliverAsync(AppNotificationEnvelope envelope, CancellationToken ct = default)
    {
        var opt = _options.CurrentValue;
        if (!opt.Enabled || string.IsNullOrWhiteSpace(opt.PublicKey) || string.IsNullOrWhiteSpace(opt.PrivateKey))
            return;

        var targets = await ResolveSubscriptionsAsync(envelope, ct);

        var payload = JsonSerializer.Serialize(new
        {
            title = envelope.Title,
            body = envelope.Body,
            url = envelope.ActionUrl,
            tag = envelope.CorrelationId.ToString("N")
        });

        foreach (var sub in targets)
        {
            try
            {
                await _delivery.SendAsync(
                    new PushDeliveryRequest(sub.Endpoint, sub.P256dh, sub.Auth, payload),
                    ct);
            }
            catch (Exception ex) when (LibWebPushDeliveryClient.IsSubscriptionGone(ex))
            {
                await _subscriptions.DeleteByIdAsync(sub.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Web Push delivery failed for subscription {SubscriptionId}", sub.Id);
            }
        }
    }

    private async Task<IReadOnlyList<PushSubscriptionDto>> ResolveSubscriptionsAsync(
        AppNotificationEnvelope envelope,
        CancellationToken ct)
    {
        switch (envelope.Audience)
        {
            case AppNotificationAudienceKind.Admins:
            {
                var adminUserIds = await _adminRecipients.ListAdminUserIdsAsync(ct);
                if (adminUserIds.Count == 0)
                    return Array.Empty<PushSubscriptionDto>();

                var merged = new List<PushSubscriptionDto>();
                foreach (var userId in adminUserIds)
                {
                    merged.AddRange(
                        await _subscriptions.ListByUserAndAudienceAsync(
                            userId,
                            PushSubscriptionAudienceFlags.AdminWebPush,
                            ct));
                }

                return merged
                    .GroupBy(s => s.Endpoint, StringComparer.Ordinal)
                    .Select(g => g.First())
                    .ToList();
            }
            case AppNotificationAudienceKind.User when envelope.TargetUserId is { } uid:
                return await _subscriptions.ListByUserAndAudienceAsync(uid, PushSubscriptionAudienceFlags.UserWebPush, ct);
            case AppNotificationAudienceKind.CompanyStakeholders when envelope.TargetCompanyId is { } companyId:
            {
                var users = await _companyRecipients.ListOwnerAndManagerUserIdsAsync(companyId, ct);
                if (users.Count == 0)
                    return Array.Empty<PushSubscriptionDto>();

                var merged = new List<PushSubscriptionDto>();
                foreach (var userId in users)
                {
                    merged.AddRange(
                        await _subscriptions.ListByUserAndAudienceAsync(
                            userId,
                            PushSubscriptionAudienceFlags.UserWebPush,
                            ct));
                }

                return merged
                    .GroupBy(s => s.Endpoint, StringComparer.Ordinal)
                    .Select(g => g.First())
                    .ToList();
            }
            default:
                return Array.Empty<PushSubscriptionDto>();
        }
    }
}
