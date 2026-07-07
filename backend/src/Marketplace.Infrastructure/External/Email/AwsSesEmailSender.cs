using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Marketplace.Application.Auth.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Email;

public sealed class AwsSesEmailSender : IEmailPort, IEmailSender, IEmailHealthProbe
{
    private readonly IAmazonSimpleEmailServiceV2 _client;
    private readonly AwsSesOptions _options;
    private readonly FrontendOptions _frontend;
    private readonly ILogger<AwsSesEmailSender> _logger;

    public AwsSesEmailSender(
        IAmazonSimpleEmailServiceV2 client,
        IOptions<AwsSesOptions> options,
        IOptions<FrontendOptions> frontend,
        ILogger<AwsSesEmailSender> logger)
    {
        _client = client;
        _options = options.Value;
        _frontend = frontend.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = _options.FromEmail,
            Destination = new Destination
            {
                ToAddresses = [to]
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject, Charset = "UTF-8" },
                    Body = new Body
                    {
                        Text = new Content { Data = body, Charset = "UTF-8" },
                        Html = new Content { Data = body, Charset = "UTF-8" }
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(_options.ConfigurationSetName))
            request.ConfigurationSetName = _options.ConfigurationSetName;

        var response = await _client.SendEmailAsync(request, ct);
        if (response.HttpStatusCode is not System.Net.HttpStatusCode.OK)
        {
            _logger.LogError(
                "AWS SES email send failed. Status={StatusCode}; To={To}; Subject={Subject}; MessageId={MessageId}",
                (int)response.HttpStatusCode,
                to,
                subject,
                response.MessageId);
            throw new InvalidOperationException($"AWS SES error: {(int)response.HttpStatusCode}");
        }
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default) =>
        SendEmailAsync(to, subject, body, ct);

    public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default)
    {
        var message = TransactionalEmailContentBuilder.BuildConfirmation(_frontend.BaseUrl, to, token);
        return SendEmailAsync(to, message.Subject, message.Body, ct);
    }

    public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default)
    {
        var message = TransactionalEmailContentBuilder.BuildPasswordReset(token);
        return SendEmailAsync(to, message.Subject, message.Body, ct);
    }

    public Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default)
    {
        var message = TransactionalEmailContentBuilder.BuildTwoFactorCode(code);
        return SendEmailAsync(to, message.Subject, message.Body, ct);
    }

    public async Task<EmailHealthStatus> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetAccountAsync(new GetAccountRequest(), ct);
            var healthy = response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            return new EmailHealthStatus(
                IsHealthy: healthy,
                Provider: "AwsSes",
                Message: healthy
                    ? $"AWS SES account reachable in {_options.Region}."
                    : $"AWS SES account check returned {(int)response.HttpStatusCode}.",
                StatusCode: (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            return new EmailHealthStatus(
                IsHealthy: false,
                Provider: "AwsSes",
                Message: $"AWS SES check failed: {ex.Message}");
        }
    }
}
