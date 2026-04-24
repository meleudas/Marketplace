using Marketplace.Application.Payments.Ports;
using Marketplace.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Tests;

public class LiqPayProviderSelectionTests
{
    [Fact]
    public void AddInfrastructure_Registers_LiqPay_Port_And_Config_Health_Reflects_Keys()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(BuildConfiguration(withLiqPayKeys: false));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var liqPay = scope.ServiceProvider.GetRequiredService<ILiqPayPort>();
        var config = liqPay.CheckConfig();

        Assert.False(config.IsHealthy);
    }

    [Fact]
    public void AddInfrastructure_LiqPay_Config_Healthy_When_Keys_Set()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(BuildConfiguration(withLiqPayKeys: true));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var liqPay = scope.ServiceProvider.GetRequiredService<ILiqPayPort>();
        var config = liqPay.CheckConfig();

        Assert.True(config.IsHealthy);
    }

    private static IConfiguration BuildConfiguration(bool withLiqPayKeys)
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

        if (withLiqPayKeys)
        {
            values["LiqPay:PublicKey"] = "public";
            values["LiqPay:PrivateKey"] = "private";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
