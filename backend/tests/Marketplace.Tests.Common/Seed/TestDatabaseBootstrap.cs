using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infra = Marketplace.Infrastructure;

namespace Marketplace.Tests.Common.Seed;

public static class TestDatabaseBootstrap
{
    public static async Task MigrateAsync(string postgresConnectionString, CancellationToken cancellationToken = default)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = postgresConnectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        Infra.DependencyInjection.AddInfrastructure(services, configuration);

        await using var scope = services.BuildServiceProvider().CreateAsyncScope();
        await Infra.DependencyInjection.InitializeDatabaseAsync(scope.ServiceProvider, cancellationToken);
    }
}
