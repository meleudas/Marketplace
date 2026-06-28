namespace Marketplace.Application.Notifications;

public sealed class AppNotificationRequest
{
    public string TemplateKey { get; init; } = string.Empty;
    public Guid CorrelationId { get; init; }
    public AppNotificationChannelKind Channels { get; init; } = AppNotificationChannelKind.Push;
    public AppNotificationAudienceKind Audience { get; init; }
    public Guid? TargetUserId { get; init; }
    public Guid? TargetCompanyId { get; init; }
    public string PayloadJson { get; init; } = "{}";
}
