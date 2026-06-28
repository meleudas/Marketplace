using System.Reflection;
using System.Text.Json;
using Marketplace.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Marketplace.Tests;

public sealed class ContractApiRoutesSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Payments")]
    [Trait("Suite", "ProductsModeration")]
    [Trait("Suite", "Reviews")]
    public void Contract_Routes_Snapshot_Matches_Critical_Endpoints()
    {
        var expected = LoadSnapshot();
        var actual = CollectControllerRoutes();

        var missing = expected.Except(actual, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0, $"Missing contract routes: {string.Join(", ", missing)}");
    }

    private static HashSet<string> CollectControllerRoutes()
    {
        var controllers = typeof(OrdersController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToArray();

        var routes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var controller in controllers)
        {
            var controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template?.Trim('/') ?? string.Empty;
            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var verb = ResolveVerb(method);
                if (verb is null)
                    continue;
                var actionRoute = method.GetCustomAttribute<RouteAttribute>()?.Template?.Trim('/')
                    ?? method.GetCustomAttributes()
                        .OfType<HttpMethodAttribute>()
                        .Select(x => x.Template?.Trim('/'))
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    ?? string.Empty;
                var path = CombineRoute(controllerRoute, actionRoute);
                routes.Add($"{verb}:{path}");
            }
        }

        return routes;
    }

    private static string? ResolveVerb(MethodInfo method)
    {
        if (method.GetCustomAttribute<HttpGetAttribute>() is not null) return "GET";
        if (method.GetCustomAttribute<HttpPostAttribute>() is not null) return "POST";
        if (method.GetCustomAttribute<HttpPutAttribute>() is not null) return "PUT";
        if (method.GetCustomAttribute<HttpPatchAttribute>() is not null) return "PATCH";
        if (method.GetCustomAttribute<HttpDeleteAttribute>() is not null) return "DELETE";
        return null;
    }

    private static string CombineRoute(string controllerRoute, string actionRoute)
    {
        if (string.IsNullOrWhiteSpace(controllerRoute))
            return actionRoute;
        if (string.IsNullOrWhiteSpace(actionRoute))
            return controllerRoute;
        return $"{controllerRoute}/{actionRoute}";
    }

    private static HashSet<string> LoadSnapshot()
    {
        var path = FindSnapshotPath();
        var json = File.ReadAllText(path);
        var routes = JsonSerializer.Deserialize<List<string>>(json) ?? [];
        return routes.ToHashSet(StringComparer.Ordinal);
    }

    private static string FindSnapshotPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Contracts", "api-routes.snapshot.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("Contract snapshot file not found: Contracts/api-routes.snapshot.json");
    }
}
