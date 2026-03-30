using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Sms;

public sealed class LoggingSmsSender : ISmsPort, ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger) => _logger = logger;

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default) =>
        SendAsync(phoneNumber, message, ct);

    public Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        _logger.LogInformation("SMS verification to {Phone}, code length {Len}", phoneNumber, code.Length);
        return Task.CompletedTask;
    }
}
