using System.Text.Json;
using System.Text.Json.Nodes;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Notifications.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class InAppNotificationChannel : INotificationChannel
{
    private readonly IInAppNotificationRepository _repository;
    private readonly IAdminNotificationRecipientIds _adminRecipients;
    private readonly ICompanyOrderNotificationRecipientIds _companyRecipients;
    private readonly IOptionsMonitor<AppNotificationOptions> _options;
    private readonly ILogger<InAppNotificationChannel> _logger;

    public InAppNotificationChannel(
        IInAppNotificationRepository repository,
        IAdminNotificationRecipientIds adminRecipients,
        ICompanyOrderNotificationRecipientIds companyRecipients,
        IOptionsMonitor<AppNotificationOptions> options,
        ILogger<InAppNotificationChannel> logger)
    {
        _repository = repository;
        _adminRecipients = adminRecipients;
        _companyRecipients = companyRecipients;
        _options = options;
        _logger = logger;
    }

    public AppNotificationChannelKind Kind => AppNotificationChannelKind.InApp;

    public async Task DeliverAsync(AppNotificationEnvelope envelope, CancellationToken ct = default)
    {
        var kind = MapKind(envelope.TemplateKey);
        var dataJson = BuildDataJson(envelope);
        var rawPayload = envelope.PayloadJson;

        switch (envelope.Audience)
        {
            case AppNotificationAudienceKind.User when envelope.TargetUserId is { } uid:
                await InsertForUserAsync(uid, envelope, kind, dataJson, rawPayload, envelope.CorrelationId, ct);
                return;

            case AppNotificationAudienceKind.Admins:
                var admins = await _adminRecipients.ListAdminUserIdsAsync(ct);
                if (admins.Count == 0)
                {
                    _logger.LogInformation("In-app admin notification skipped: no admin users found.");
                    return;
                }

                foreach (var adminId in admins)
                {
                    var perUserCorrelation = InAppNotificationCorrelation.PerUser(envelope.CorrelationId, adminId);
                    await InsertForUserAsync(adminId, envelope, kind, dataJson, rawPayload, perUserCorrelation, ct);
                }

                return;

            case AppNotificationAudienceKind.CompanyStakeholders when envelope.TargetCompanyId is { } companyId:
                var stakeholders = await _companyRecipients.ListOwnerAndManagerUserIdsAsync(companyId, ct);
                if (stakeholders.Count == 0)
                {
                    _logger.LogInformation("In-app company notification skipped: no owner/manager for company {CompanyId}.", companyId);
                    return;
                }

                foreach (var userId in stakeholders)
                {
                    var perUserCorrelation = InAppNotificationCorrelation.PerUser(envelope.CorrelationId, userId);
                    await InsertForUserAsync(userId, envelope, kind, dataJson, rawPayload, perUserCorrelation, ct);
                }

                return;

            default:
                _logger.LogDebug("In-app notification skipped for audience {Audience}", envelope.Audience);
                return;
        }
    }

    private async Task InsertForUserAsync(
        Guid userId,
        AppNotificationEnvelope envelope,
        NotificationKind kind,
        string dataJson,
        string? rawPayload,
        Guid correlationForRow,
        CancellationToken ct)
    {
        var ttlDays = _options.CurrentValue.InAppDefaultTtlDays;
        DateTime? expiresAtUtc = ttlDays > 0 ? DateTime.UtcNow.AddDays(ttlDays) : null;

        var inserted = await _repository.TryInsertAsync(
            userId,
            kind,
            envelope.Title,
            envelope.Body,
            dataJson,
            envelope.ActionUrl,
            correlationForRow,
            expiresAtUtc,
            rawPayload,
            ct);

        if (!inserted)
            _logger.LogDebug("In-app notification deduplicated for user {UserId}", userId);
    }

    private static NotificationKind MapKind(string templateKey) =>
        templateKey switch
        {
            AppNotificationTemplateKeys.AdminNewOrder
                or AppNotificationTemplateKeys.CompanyNewOrder
                or AppNotificationTemplateKeys.UserOrderStatus => NotificationKind.Order,
            AppNotificationTemplateKeys.UserPaymentStatus => NotificationKind.Payment,
            AppNotificationTemplateKeys.CartProductBackInStock => NotificationKind.System,
            AppNotificationTemplateKeys.AdminProductPendingReview
                or AppNotificationTemplateKeys.UserProductApproved
                or AppNotificationTemplateKeys.UserProductRejected
                or AppNotificationTemplateKeys.ChatMessageReceived => NotificationKind.System,
            _ => NotificationKind.System
        };

    private static string BuildDataJson(AppNotificationEnvelope envelope)
    {
        JsonObject root;
        try
        {
            var node = JsonNode.Parse(string.IsNullOrWhiteSpace(envelope.PayloadJson) ? "{}" : envelope.PayloadJson);
            root = node as JsonObject ?? new JsonObject { ["payload"] = node };
        }
        catch
        {
            root = new JsonObject();
        }

        root["templateKey"] = envelope.TemplateKey;
        root["templateVersion"] = envelope.TemplateVersion;
        root["jobCorrelationId"] = envelope.CorrelationId.ToString("N");
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
