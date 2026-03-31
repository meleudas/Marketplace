namespace Marketplace.Application.Auth.Ports;

public interface ITelegramPort
{
    Task SendMessageAsync(string chatId, string message, CancellationToken ct = default);
}
