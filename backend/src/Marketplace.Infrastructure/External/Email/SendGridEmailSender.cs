using Marketplace.Application.Auth.Ports;
using Marketplace.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Marketplace.Infrastructure.External.Email;

public sealed class SendGridEmailSender : IEmailPort, IEmailSender, IEmailHealthProbe
{
    private readonly ILogger<SendGridEmailSender> _logger;
    private readonly SendGridClient _client;
    private readonly EmailAddress _from;
    private readonly FrontendOptions _frontend;

    public SendGridEmailSender(
        ILogger<SendGridEmailSender> logger,
        IOptions<SendGridOptions> options,
        IOptions<FrontendOptions> frontend)
    {
        _logger = logger;
        var cfg = options.Value;
        _client = new SendGridClient(cfg.ApiKey);
        _from = new EmailAddress(cfg.FromEmail, cfg.FromName);
        _frontend = frontend.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var message = MailHelper.CreateSingleEmail(_from, new EmailAddress(to), subject, plainTextContent: body, htmlContent: body);
        var response = await _client.SendEmailAsync(message, ct);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError("SendGrid email send failed. Status={StatusCode}; To={To}; Subject={Subject}; Body={Body}",
                (int)response.StatusCode, to, subject, responseBody);
            throw new InvalidOperationException($"SendGrid error: {(int)response.StatusCode}");
        }
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default) =>
        SendEmailAsync(to, subject, body, ct);

    public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default)
    {
        var link = EmailConfirmationLinkBuilder.Build(_frontend.BaseUrl, to, token);
        var body =
            "Підтвердіть email, перейшовши за посиланням:\n" + link + "\n\n" +
            "Якщо ви не реєструвались, проігноруйте цей лист.";
        return SendEmailAsync(to, "Підтвердження email", body, ct);
    }

    public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default)
    {
        var body =
            $"Ваш код для скидання пароля: {token}\n\n" +
            "Якщо ви не запитували скидання, змініть пароль і перевірте безпеку акаунта.";
        return SendEmailAsync(to, "Скидання пароля", body, ct);
    }

    public Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default)
    {
        var body =
            $"Ваш код 2FA: {code}\n\n" +
            "Код одноразовий. Нікому його не повідомляйте.";
        return SendEmailAsync(to, "Код двофакторної автентифікації", body, ct);
    }

    public async Task<EmailHealthStatus> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _client.RequestAsync(
                method: BaseClient.Method.GET,
                urlPath: "scopes",
                cancellationToken: ct);

            if (response.IsSuccessStatusCode)
            {
                return new EmailHealthStatus(
                    IsHealthy: true,
                    Provider: "SendGrid",
                    Message: "SendGrid API authentication succeeded.",
                    StatusCode: (int)response.StatusCode);
            }

            var body = await response.Body.ReadAsStringAsync(ct);
            return new EmailHealthStatus(
                IsHealthy: false,
                Provider: "SendGrid",
                Message: $"SendGrid API auth failed: {body}",
                StatusCode: (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return new EmailHealthStatus(
                IsHealthy: false,
                Provider: "SendGrid",
                Message: $"SendGrid check failed: {ex.Message}");
        }
    }
}
