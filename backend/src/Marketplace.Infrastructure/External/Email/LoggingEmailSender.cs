using Marketplace.Application.Auth.Ports;
using Marketplace.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Email;

public sealed class LoggingEmailSender : IEmailPort, IEmailSender, IEmailHealthProbe
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly FrontendOptions _frontend;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IOptions<FrontendOptions> frontend)
    {
        _logger = logger;
        _frontend = frontend.Value;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default) =>
        SendAsync(to, subject, body, ct);

    public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default)
    {
        var message = TransactionalEmailContentBuilder.BuildConfirmation(_frontend.BaseUrl, to, token);
        _logger.LogInformation(
            "Confirmation email to {To}, link embedded in body",
            to);
        return SendAsync(to, message.Subject, message.Body, ct);
    }

    public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default)
    {
        var message = TransactionalEmailContentBuilder.BuildPasswordReset(token);
        _logger.LogInformation("Password reset email to {To}, token length {Len}", to, token.Length);
        return SendAsync(to, message.Subject, message.Body, ct);
    }

    public Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default)
    {
        var message = TransactionalEmailContentBuilder.BuildTwoFactorCode(code);
        _logger.LogInformation("2FA email to {To}, code length {Len}", to, code.Length);
        return SendAsync(to, message.Subject, message.Body, ct);
    }

    public Task<EmailHealthStatus> CheckAsync(CancellationToken ct = default)
        => Task.FromResult(new EmailHealthStatus(
            IsHealthy: true,
            Provider: "LoggingEmailSender",
            Message: "No external email provider configured. Using logging email sender."));
}
