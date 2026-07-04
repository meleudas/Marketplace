using Marketplace.Application.Auth.Ports;
using Marketplace.Infrastructure;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.External.Telegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Tests;

public class EmailProviderSelectionTests
{
    [Fact]
    public void AddInfrastructure_Uses_LoggingEmailSender_When_No_Provider_Configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration();

        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var emailPort = scope.ServiceProvider.GetRequiredService<IEmailPort>();

        Assert.IsType<LoggingEmailSender>(emailPort);
    }

    [Fact]
    public void AddInfrastructure_Uses_SendGridEmailSender_When_SendGrid_Configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(includeSendGrid: true);

        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var emailPort = scope.ServiceProvider.GetRequiredService<IEmailPort>();

        Assert.IsType<SendGridEmailSender>(emailPort);
    }

    [Fact]
    public void AddInfrastructure_Uses_AwsSesEmailSender_When_Ses_Configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(includeSes: true);

        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var emailPort = scope.ServiceProvider.GetRequiredService<IEmailPort>();

        Assert.IsType<AwsSesEmailSender>(emailPort);
    }

    [Fact]
    public void AddInfrastructure_Prefers_AwsSes_When_Ses_And_SendGrid_Configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(includeSendGrid: true, includeSes: true);

        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var emailPort = scope.ServiceProvider.GetRequiredService<IEmailPort>();

        Assert.IsType<AwsSesEmailSender>(emailPort);
    }

    [Fact]
    public void AddInfrastructure_Uses_LoggingTelegramSender_When_Telegram_Not_Configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(includeTelegram: false);

        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var telegramPort = scope.ServiceProvider.GetRequiredService<ITelegramPort>();

        Assert.IsType<LoggingTelegramSender>(telegramPort);
    }

    [Fact]
    public void AddInfrastructure_Uses_TelegramBotSender_When_Telegram_Configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(includeTelegram: true);

        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var telegramPort = scope.ServiceProvider.GetRequiredService<ITelegramPort>();

        Assert.IsType<TelegramBotSender>(telegramPort);
    }

    private static IConfiguration BuildConfiguration(
        bool includeSendGrid = false,
        bool includeSes = false,
        bool includeTelegram = false)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Port=5432;Database=marketplace;Username=postgres;Password=postgres",
            ["Jwt:SecretKey"] = "ChangeThis_DevSecretKey_AtLeast32CharsLong!!",
            ["Jwt:Issuer"] = "Marketplace",
            ["Jwt:Audience"] = "Marketplace",
            ["Jwt:AccessTokenMinutes"] = "10",
            ["Jwt:RefreshTokenDays"] = "30"
        };

        if (includeSendGrid)
        {
            values["SendGrid:ApiKey"] = "SG.mocked-key";
            values["SendGrid:FromEmail"] = "verified@example.com";
            values["SendGrid:FromName"] = "Marketplace";
        }

        if (includeSes)
        {
            values["AwsSes:Enabled"] = "true";
            values["AwsSes:Region"] = "eu-central-1";
            values["AwsSes:FromEmail"] = "noreply@example.com";
            values["AwsSes:FromName"] = "Marketplace";
        }

        if (includeTelegram)
        {
            values["Telegram:BotToken"] = "mock-bot-token";
            values["Telegram:WebhookSecret"] = "secret";
            values["Telegram:LinkCodeTtlMinutes"] = "10";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
