using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Marketplace.Infrastructure;
using Marketplace.Infrastructure.External.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Marketplace.Tests;

public sealed class AwsSesEmailSenderTests
{
    [Fact]
    public async Task SendEmailAsync_Calls_Ses_With_Expected_Request()
    {
        var client = new Mock<IAmazonSimpleEmailServiceV2>();
        client
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                MessageId = "msg-1"
            });

        var sender = new AwsSesEmailSender(
            client.Object,
            Options.Create(new AwsSesOptions
            {
                Enabled = true,
                Region = "eu-central-1",
                FromEmail = "noreply@example.com",
                FromName = "Marketplace"
            }),
            Options.Create(new FrontendOptions { BaseUrl = "http://localhost:3000" }),
            NullLogger<AwsSesEmailSender>.Instance);

        await sender.SendEmailAsync("user@example.com", "Subject", "Body", CancellationToken.None);

        client.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(r =>
                r.FromEmailAddress == "noreply@example.com" &&
                r.Destination.ToAddresses.Single() == "user@example.com" &&
                r.Content.Simple.Subject.Data == "Subject" &&
                r.Content.Simple.Body.Text!.Data == "Body"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendConfirmationEmailAsync_Uses_Shared_Template()
    {
        var client = new Mock<IAmazonSimpleEmailServiceV2>();
        SendEmailRequest? captured = null;
        client
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SendEmailResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        var sender = new AwsSesEmailSender(
            client.Object,
            Options.Create(new AwsSesOptions
            {
                Enabled = true,
                Region = "eu-central-1",
                FromEmail = "noreply@example.com"
            }),
            Options.Create(new FrontendOptions { BaseUrl = "http://localhost:3000" }),
            NullLogger<AwsSesEmailSender>.Instance);

        await sender.SendConfirmationEmailAsync("user@example.com", "token-123", CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Підтвердження email", captured!.Content.Simple.Subject.Data);
        Assert.Contains("confirm-email", captured.Content.Simple.Body.Text!.Data, StringComparison.Ordinal);
    }
}
