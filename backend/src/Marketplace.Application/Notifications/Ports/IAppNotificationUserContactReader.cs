namespace Marketplace.Application.Notifications.Ports;

/// <summary>Контактні дані користувача для застосункових Email/Telegram (Identity).</summary>
public sealed record AppNotificationUserContact(
    string? Email,
    bool EmailConfirmed,
    string? TelegramChatId,
    bool NotifyAppByEmail,
    bool NotifyAppByTelegram);

public interface IAppNotificationUserContactReader
{
    Task<AppNotificationUserContact?> GetAsync(Guid userId, CancellationToken ct = default);
}
