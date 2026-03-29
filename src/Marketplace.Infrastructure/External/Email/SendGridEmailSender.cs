using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Email;

/// <summary>Заглушка під SendGrid; підключіть SDK та секрети перед використанням у продакшені.</summary>
public sealed class SendGridEmailSender : IEmailPort
{
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(ILogger<SendGridEmailSender> logger) => _logger = logger;

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogWarning("SendGridEmailSender: не налаштовано — лист не надіслано ({To}, {Subject})", to, subject);
        return Task.CompletedTask;
    }

    public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default)
    {
        _logger.LogWarning("SendGridEmailSender: confirmation не реалізовано ({To})", to);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default)
    {
        _logger.LogWarning("SendGridEmailSender: password reset не реалізовано ({To})", to);
        return Task.CompletedTask;
    }
}
