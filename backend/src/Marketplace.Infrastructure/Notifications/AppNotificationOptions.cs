namespace Marketplace.Infrastructure.Notifications;

public sealed class AppNotificationOptions
{
    public const string SectionName = "AppNotifications";

    /// <summary>Транзакційні листи маркетплейсу через <c>IEmailPort.SendEmailAsync</c>.</summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>Повідомлення в Telegram через <c>ITelegramPort</c> (окремо від 2FA).</summary>
    public bool TelegramEnabled { get; set; } = true;

    public bool SmsEnabled { get; set; } = false;

    /// <summary>Термін зберігання in-app рядка; 0 = без закінчення.</summary>
    public int InAppDefaultTtlDays { get; set; } = 90;

    public string EmailSubjectPrefix { get; set; } = "[Marketplace]";

    public bool PruneExpiredInAppEnabled { get; set; } = true;
}
