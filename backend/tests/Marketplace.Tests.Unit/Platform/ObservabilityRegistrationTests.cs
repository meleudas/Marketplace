using Marketplace.API.Extensions;
using Marketplace.API.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Marketplace.Tests.Observability;

public sealed class ObservabilityRegistrationTests
{
    [Fact]
    [Trait("Suite", "Platform")]
    public void OpenTelemetry_providers_registered_when_enabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:Enabled"] = "true",
                ["ConnectionStrings:Redis"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        var hostEnvironment = new HostEnvironmentStub();

        services.AddMarketplaceOpenTelemetry(configuration, hostEnvironment);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MeterProvider>());
        Assert.NotNull(provider.GetService<TracerProvider>());
    }

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Marketplace.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
