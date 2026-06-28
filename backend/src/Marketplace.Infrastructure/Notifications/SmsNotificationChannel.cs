using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class SmsNotificationChannel : INotificationChannel
{
    private readonly ISmsPort _sms;
    private readonly IAppNotificationUserContactReader _contacts;
    private readonly IOptionsMonitor<AppNotificationOptions> _options;
    private readonly ILogger<SmsNotificationChannel> _logger;

    public SmsNotificationChannel(
        ISmsPort sms,
        IAppNotificationUserContactReader contacts,
        IOptionsMonitor<AppNotificationOptions> options,
        ILogger<SmsNotificationChannel> logger)
    {
        _sms = sms;
        _contacts = contacts;
        _options = options;
        _logger = logger;
    }

    public AppNotificationChannelKind Kind => AppNotificationChannelKind.Sms;

    public async Task DeliverAsync(AppNotificationEnvelope envelope, CancellationToken ct = default)
    {
        if (!_options.CurrentValue.SmsEnabled)
        {
            _logger.LogDebug("App SMS channel disabled; skipping template {Template}", envelope.TemplateKey);
            return;
        }

        if (envelope.Audience != AppNotificationAudienceKind.User || envelope.TargetUserId is not { } userId)
        {
            _logger.LogDebug("SMS notification skipped for audience {Audience}", envelope.Audience);
            return;
        }

        var contact = await _contacts.GetAsync(userId, ct);
        if (contact is null || string.IsNullOrWhiteSpace(contact.PhoneNumber) || !contact.PhoneNumberConfirmed)
        {
            _logger.LogDebug("User {UserId} has no confirmed phone; skipping app SMS.", userId);
            return;
        }

        var message = AppNotificationMessageFormatter.BuildPlainBody(envelope.Title, envelope.Body, envelope.ActionUrl);
        await _sms.SendSmsAsync(contact.PhoneNumber, message, ct);
    }
}
