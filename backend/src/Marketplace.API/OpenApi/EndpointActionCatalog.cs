using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Marketplace.API.OpenApi;

public static class EndpointActionCatalog
{
    public static IReadOnlyList<(string Method, string Path)> DiscoverFromAssembly(Assembly assembly)
    {
        var endpoints = new List<(string Method, string Path)>();

        foreach (var controller in assembly.GetTypes().Where(IsApiController))
        {
            var controllerRoutePrefix = controller
                .GetCustomAttributes<RouteAttribute>(inherit: true)
                .Select(static attribute => attribute.Template)
                .FirstOrDefault(template => !string.IsNullOrWhiteSpace(template));

            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var httpMethod in ResolveHttpMethods(method))
                {
                    foreach (var routeTemplate in ResolveRouteTemplates(method, controllerRoutePrefix))
                    {
                        endpoints.Add((httpMethod, EndpointPathNormalizer.NormalizePath(routeTemplate)));
                    }
                }
            }
        }

        return endpoints
            .Distinct()
            .OrderBy(static endpoint => endpoint.Path, StringComparer.Ordinal)
            .ThenBy(static endpoint => endpoint.Method, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsApiController(Type type) =>
        type is { IsAbstract: false, IsPublic: true }
        && type.Name.EndsWith("Controller", StringComparison.Ordinal)
        && type.GetCustomAttribute<ApiControllerAttribute>() is not null;

    private static IEnumerable<string> ResolveHttpMethods(MethodInfo method)
    {
        if (method.GetCustomAttribute<HttpGetAttribute>() is not null) yield return "GET";
        if (method.GetCustomAttribute<HttpPostAttribute>() is not null) yield return "POST";
        if (method.GetCustomAttribute<HttpPutAttribute>() is not null) yield return "PUT";
        if (method.GetCustomAttribute<HttpPatchAttribute>() is not null) yield return "PATCH";
        if (method.GetCustomAttribute<HttpDeleteAttribute>() is not null) yield return "DELETE";
    }

    private static IEnumerable<string> ResolveRouteTemplates(MethodInfo method, string? controllerRoutePrefix)
    {
        var httpMethodAttributes = method
            .GetCustomAttributes(inherit: true)
            .OfType<HttpMethodAttribute>()
            .ToArray();

        if (httpMethodAttributes.Length == 0)
            yield break;

        foreach (var attribute in httpMethodAttributes)
        {
            yield return CombineRoutes(controllerRoutePrefix, attribute.Template);
        }
    }

    private static string CombineRoutes(string? prefix, string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return "/";

            return prefix.Trim('/');
        }

        if (string.IsNullOrWhiteSpace(prefix))
            return template.Trim('/');

        if (template.StartsWith('/'))
            return template.TrimStart('/');

        return $"{prefix.Trim('/')}/{template.Trim('/')}";
    }
}
