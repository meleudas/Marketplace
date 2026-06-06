using System.Text.RegularExpressions;

namespace Marketplace.API.OpenApi;

public static partial class EndpointPathNormalizer
{
    [GeneratedRegex(@"\{([^}:]+)(?::[^}]+)?\}", RegexOptions.CultureInvariant)]
    private static partial Regex RouteParameterRegex();

    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var normalized = path.Trim().TrimEnd('/');
        var queryIndex = normalized.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
            normalized = normalized[..queryIndex];

        var fragmentIndex = normalized.IndexOf('#', StringComparison.Ordinal);
        if (fragmentIndex >= 0)
            normalized = normalized[..fragmentIndex];
        if (normalized.Length == 0)
            normalized = "/";
        if (!normalized.StartsWith('/'))
            normalized = "/" + normalized;

        normalized = RouteParameterRegex().Replace(normalized, "{$1}");
        return normalized.ToLowerInvariant();
    }

    public static string NormalizeMethod(string? method) =>
        string.IsNullOrWhiteSpace(method)
            ? "GET"
            : method.Trim().ToUpperInvariant();
}
