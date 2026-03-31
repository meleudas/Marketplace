using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Telegram;

public sealed class LoggingTelegramSender : ITelegramPort
{
    private readonly ILogger<LoggingTelegramSender> _logger;

    public LoggingTelegramSender(ILogger<LoggingTelegramSender> logger)
    {
        _logger = logger;
    }

    public Task SendMessageAsync(string chatId, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("Telegram disabled. Message to chat {ChatId}: {Message}", chatId, message);
        return Task.CompletedTask;
    }
}
