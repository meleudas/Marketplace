namespace Marketplace.Application.Auth.Ports;

public interface INotificationDispatcher
{
    Task EnqueueConfirmationEmailAsync(string to, string token, CancellationToken ct = default);
    Task EnqueuePasswordResetEmailAsync(string to, string token, CancellationToken ct = default);
    Task EnqueueTwoFactorEmailAsync(string to, string code, CancellationToken ct = default);
    Task EnqueueTelegramMessageAsync(string chatId, string message, CancellationToken ct = default);
    Task EnqueueSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
}
