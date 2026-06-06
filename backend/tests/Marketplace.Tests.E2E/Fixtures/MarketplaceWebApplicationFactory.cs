using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Marketplace.Tests.Fixtures;

public sealed class MarketplaceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> _settings;

    public MarketplaceWebApplicationFactory(IReadOnlyDictionary<string, string?> settings) => _settings = settings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Testing — щоб Program.cs не викликав повторну міграцію при CreateClient (лише фікстура).
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_settings));
    }

    public async Task InitializeDatabaseAsync()
    {
        if (Server is null)
            _ = CreateClient();

        await using var scope = Services.CreateAsyncScope();
        await Marketplace.Infrastructure.DependencyInjection.InitializeDatabaseAsync(scope.ServiceProvider);
    }
}
