namespace Marketplace.Application.Notifications.Options;

public sealed class NotificationDispatchAntiAbuseOptions
{
    public const string SectionName = "NotificationDispatchAntiAbuse";

    public int BurstPerTargetPerMinute { get; set; } = 30;
    public int BurstWindowSeconds { get; set; } = 60;
}
