using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marketplace.Tests.Fixtures;

internal static class E2EJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
