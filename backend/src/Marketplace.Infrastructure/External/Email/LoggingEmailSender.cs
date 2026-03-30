using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Email;

public sealed class LoggingEmailSender : IEmailPort, IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default) =>
        SendAsync(to, subject, body, ct);

    public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Confirmation email to {To}, token length {Len}", to, token.Length);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Password reset email to {To}, token length {Len}", to, token.Length);
        return Task.CompletedTask;
    }

    public Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default)
    {
        _logger.LogInformation("2FA email to {To}, code length {Len}", to, code.Length);
        return Task.CompletedTask;
    }
}
