using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.Application.Common.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Extensions;

public static class HttpIdempotencyExtensions
{
    public static bool TryGetIdempotencyKey(this HttpRequest request, out string key)
    {
        key = request.Headers.TryGetValue("Idempotency-Key", out var values)
            ? values.ToString().Trim()
            : string.Empty;
        return !string.IsNullOrWhiteSpace(key);
    }

    public static string BuildRequestHash(params string?[] values)
    {
        var source = string.Join('|', values.Select(x => x ?? string.Empty));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(bytes);
    }

    public static IActionResult ReplayResponse(this ControllerBase controller, HttpIdempotencyStoredResponse stored)
    {
        if (string.IsNullOrWhiteSpace(stored.ResponseBodyJson))
            return new StatusCodeResult(stored.StatusCode);

        using var doc = JsonDocument.Parse(stored.ResponseBodyJson);
        return new ObjectResult(doc.RootElement.Clone()) { StatusCode = stored.StatusCode };
    }

    public static (int StatusCode, string? BodyJson) SnapshotResult(this IActionResult result)
    {
        return result switch
        {
            OkResult => (StatusCodes.Status200OK, null),
            OkObjectResult okObj => (okObj.StatusCode ?? StatusCodes.Status200OK, JsonSerializer.Serialize(okObj.Value)),
            ObjectResult obj => (obj.StatusCode ?? StatusCodes.Status200OK, JsonSerializer.Serialize(obj.Value)),
            StatusCodeResult sc => (sc.StatusCode, null),
            _ => (StatusCodes.Status200OK, null)
        };
    }
}
