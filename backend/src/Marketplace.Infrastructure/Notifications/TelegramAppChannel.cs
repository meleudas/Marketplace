using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class TelegramAppChannel : INotificationChannel
{
    private readonly ITelegramPort _telegram;
    private readonly IAppNotificationUserContactReader _contacts;
    private readonly IAdminNotificationRecipientIds _adminRecipients;
    private readonly ICompanyOrderNotificationRecipientIds _companyRecipients;
    private readonly IOptionsMonitor<AppNotificationOptions> _options;
    private readonly ILogger<TelegramAppChannel> _logger;

    public TelegramAppChannel(
        ITelegramPort telegram,
        IAppNotificationUserContactReader contacts,
        IAdminNotificationRecipientIds adminRecipients,
        ICompanyOrderNotificationRecipientIds companyRecipients,
        IOptionsMonitor<AppNotificationOptions> options,
        ILogger<TelegramAppChannel> logger)
    {
        _telegram = telegram;
        _contacts = contacts;
        _adminRecipients = adminRecipients;
        _companyRecipients = companyRecipients;
        _options = options;
        _logger = logger;
    }

    public AppNotificationChannelKind Kind => AppNotificationChannelKind.Telegram;

    public async Task DeliverAsync(AppNotificationEnvelope envelope, CancellationToken ct = default)
    {
        if (!_options.CurrentValue.TelegramEnabled)
        {
            _logger.LogDebug("App Telegram channel disabled; skipping template {Template}", envelope.TemplateKey);
            return;
        }

        var plain = AppNotificationMessageFormatter.BuildPlainBody(envelope.Title, envelope.Body, envelope.ActionUrl);
        var text = AppNotificationMessageFormatter.TruncateForTelegram(plain);

        switch (envelope.Audience)
        {
            case AppNotificationAudienceKind.User when envelope.TargetUserId is { } uid:
                await TrySendToUserAsync(uid, text, ct);
                return;

            case AppNotificationAudienceKind.Admins:
                var admins = await _adminRecipients.ListAdminUserIdsAsync(ct);
                foreach (var id in admins)
                    await TrySendToUserAsync(id, text, ct);
                return;

            case AppNotificationAudienceKind.CompanyStakeholders when envelope.TargetCompanyId is { } companyId:
                var stakeholders = await _companyRecipients.ListOwnerAndManagerUserIdsAsync(companyId, ct);
                foreach (var id in stakeholders)
                    await TrySendToUserAsync(id, text, ct);
                return;

            default:
                _logger.LogDebug("Telegram app notification skipped for audience {Audience}", envelope.Audience);
                return;
        }
    }

    private async Task TrySendToUserAsync(Guid userId, string text, CancellationToken ct)
    {
        var c = await _contacts.GetAsync(userId, ct);
        if (c is null)
            return;
        if (!c.NotifyAppByTelegram)
        {
            _logger.LogDebug("User {UserId} opted out of app Telegram notifications.", userId);
            return;
        }

        if (string.IsNullOrWhiteSpace(c.TelegramChatId))
        {
            _logger.LogDebug("User {UserId} has no Telegram chat linked; skipping app Telegram.", userId);
            return;
        }

        try
        {
            await _telegram.SendMessageAsync(c.TelegramChatId.Trim(), text, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "App Telegram send failed for user {UserId}", userId);
            throw;
        }
    }
}
