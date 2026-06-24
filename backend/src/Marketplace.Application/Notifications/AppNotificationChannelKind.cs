namespace Marketplace.Application.Notifications;

[Flags]
public enum AppNotificationChannelKind
{
    None = 0,
    Push = 1,
    InApp = 2,
    Email = 4,
    Telegram = 8,
    Sms = 16
}
