using Marketplace.API.Controllers;
using Marketplace.API.OpenApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Marketplace.Tests.Platform;

public sealed class EndpointDocumentationCoverageTests
{
    [Fact]
    [Trait("Suite", "Platform")]
    public void Every_controller_action_has_markdown_documentation()
    {
        var registry = CreateRegistry();
        var discovered = EndpointActionCatalog.DiscoverFromAssembly(typeof(OrdersController).Assembly);
        var missing = registry.GetMissingKeys(discovered);

        Assert.True(
            missing.Count == 0,
            "Missing endpoint docs:\n" + string.Join('\n', missing.Select(static key => $"{key.Method} {key.Path}")));
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void Documented_cart_checkout_has_summary_and_description()
    {
        var registry = CreateRegistry();

        Assert.True(registry.TryGet("POST", "/me/cart/checkout", out var entry));
        Assert.False(string.IsNullOrWhiteSpace(entry!.Summary));
        Assert.Contains("Призначення", entry.BuildDescriptionMarkdown());
        Assert.Contains("Async", entry.BuildDescriptionMarkdown());
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void Documented_admin_catalog_has_summary_and_description()
    {
        var registry = CreateRegistry();

        Assert.True(registry.TryGet("GET", "/admin/companies", out var entry));
        Assert.False(string.IsNullOrWhiteSpace(entry!.Summary));
        Assert.Contains("Хто може викликати", entry.BuildDescriptionMarkdown());
    }

    private static EndpointDocRegistry CreateRegistry()
    {
        var environment = new HostEnvironmentStub
        {
            ContentRootPath = ResolveApiContentRoot()
        };

        return new EndpointDocRegistry(environment);
    }

    private static string ResolveApiContentRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "Marketplace.API");
            if (Directory.Exists(Path.Combine(candidate, "Docs", "Endpoints")))
                return candidate;

            candidate = current.FullName;
            if (Directory.Exists(Path.Combine(candidate, "Docs", "Endpoints")))
                return candidate;

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate Marketplace.API content root.");
    }

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Marketplace.API";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
