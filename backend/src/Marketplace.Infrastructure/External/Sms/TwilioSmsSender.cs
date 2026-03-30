using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Sms;

/// <summary>Заглушка під Twilio; додайте пакет Twilio SDK та credentials.</summary>
public sealed class TwilioSmsSender : ISmsPort
{
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(ILogger<TwilioSmsSender> logger) => _logger = logger;

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _logger.LogWarning("TwilioSmsSender: не налаштовано ({Phone})", phoneNumber);
        return Task.CompletedTask;
    }

    public Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        _logger.LogWarning("TwilioSmsSender: verification не реалізовано ({Phone})", phoneNumber);
        return Task.CompletedTask;
    }
}
