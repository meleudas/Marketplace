using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly IEmailPort _email;
    private readonly IAppNotificationUserContactReader _contacts;
    private readonly IAdminNotificationRecipientIds _adminRecipients;
    private readonly ICompanyOrderNotificationRecipientIds _companyRecipients;
    private readonly IOptionsMonitor<AppNotificationOptions> _options;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(
        IEmailPort email,
        IAppNotificationUserContactReader contacts,
        IAdminNotificationRecipientIds adminRecipients,
        ICompanyOrderNotificationRecipientIds companyRecipients,
        IOptionsMonitor<AppNotificationOptions> options,
        ILogger<EmailNotificationChannel> logger)
    {
        _email = email;
        _contacts = contacts;
        _adminRecipients = adminRecipients;
        _companyRecipients = companyRecipients;
        _options = options;
        _logger = logger;
    }

    public AppNotificationChannelKind Kind => AppNotificationChannelKind.Email;

    public async Task DeliverAsync(AppNotificationEnvelope envelope, CancellationToken ct = default)
    {
        if (!_options.CurrentValue.EmailEnabled)
        {
            _logger.LogDebug("App email channel disabled; skipping template {Template}", envelope.TemplateKey);
            return;
        }

        var prefix = (_options.CurrentValue.EmailSubjectPrefix ?? "[Marketplace]").Trim();
        if (prefix.Length > 0 && !prefix.EndsWith(" ", StringComparison.Ordinal))
            prefix += " ";
        var subject = $"{prefix}{envelope.Title}".Trim();
        var body = AppNotificationMessageFormatter.BuildPlainBody(envelope.Title, envelope.Body, envelope.ActionUrl);

        switch (envelope.Audience)
        {
            case AppNotificationAudienceKind.User when envelope.TargetUserId is { } uid:
                await TrySendToUserAsync(uid, subject, body, ct);
                return;

            case AppNotificationAudienceKind.Admins:
                var admins = await _adminRecipients.ListAdminUserIdsAsync(ct);
                foreach (var id in admins)
                    await TrySendToUserAsync(id, subject, body, ct);
                return;

            case AppNotificationAudienceKind.CompanyStakeholders when envelope.TargetCompanyId is { } companyId:
                var stakeholders = await _companyRecipients.ListOwnerAndManagerUserIdsAsync(companyId, ct);
                foreach (var id in stakeholders)
                    await TrySendToUserAsync(id, subject, body, ct);
                return;

            default:
                _logger.LogDebug("Email notification skipped for audience {Audience}", envelope.Audience);
                return;
        }
    }

    private async Task TrySendToUserAsync(Guid userId, string subject, string body, CancellationToken ct)
    {
        var c = await _contacts.GetAsync(userId, ct);
        if (c is null)
            return;
        if (!c.NotifyAppByEmail)
        {
            _logger.LogDebug("User {UserId} opted out of app email notifications.", userId);
            return;
        }

        if (string.IsNullOrWhiteSpace(c.Email) || !c.EmailConfirmed)
        {
            _logger.LogDebug("User {UserId} has no confirmed email; skipping app email.", userId);
            return;
        }

        try
        {
            await _email.SendEmailAsync(c.Email, subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "App email send failed for user {UserId}", userId);
            throw;
        }
    }
}
