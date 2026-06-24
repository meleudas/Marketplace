namespace Marketplace.Application.Notifications;

public sealed class AppNotificationEnvelope
{
    public string TemplateKey { get; init; } = string.Empty;
    public int TemplateVersion { get; init; } = 1;
    public Guid CorrelationId { get; init; }
    public AppNotificationChannelKind Channels { get; init; }
    public AppNotificationAudienceKind Audience { get; init; }
    public Guid? TargetUserId { get; init; }
    public Guid? TargetCompanyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string PayloadJson { get; init; } = "{}";
}
