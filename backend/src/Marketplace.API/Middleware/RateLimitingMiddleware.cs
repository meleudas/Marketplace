using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marketplace.API.Middleware;

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitCounterStore _store;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitCounterStore store,
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var rule = ResolveRule(context);
        if (rule is null)
        {
            await _next(context);
            return;
        }

        var (group, policy, partitionKey) = rule.Value;
        if (!await AcquireAsync(group, policy, partitionKey, context))
            return;

        if (group == "auth" && IsAuthEmailEndpoint(context.Request.Path))
        {
            var emailKey = await TryReadEmailPartitionAsync(context);
            if (!string.IsNullOrWhiteSpace(emailKey)
                && !await AcquireAsync("auth-email", _options.AuthEmail, emailKey, context))
                return;
        }

        if (group == "password-reset")
        {
            var emailKey = await TryReadEmailPartitionAsync(context);
            if (!string.IsNullOrWhiteSpace(emailKey)
                && !await AcquireAsync("password-reset-email", _options.PasswordReset, emailKey, context))
                return;
        }

        await _next(context);
    }

    private async Task<bool> AcquireAsync(string group, RateLimitPolicyOptions policy, string partitionKey, HttpContext context)
    {
        var result = await _store.TryAcquireAsync(
            $"{group}:{partitionKey}",
            TimeSpan.FromSeconds(Math.Max(1, policy.WindowSeconds)),
            Math.Max(1, policy.PermitLimit),
            context.RequestAborted);

        if (result.Allowed)
            return true;

        MarketplaceMetrics.RateLimitRejected.Add(1, new KeyValuePair<string, object?>("group", group));
        _logger.LogWarning("Rate limit exceeded for group {Group} key {Key}", group, partitionKey);
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = result.RetryAfterSeconds.ToString();
        await context.Response.WriteAsJsonAsync(new { error = "Too many requests", retryAfterSeconds = result.RetryAfterSeconds });
        return false;
    }

    private (string Group, RateLimitPolicyOptions Policy, string PartitionKey)? ResolveRule(HttpContext context)
    {
        if (!string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            return null;

        var p = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (p is "/auth/register" or "/auth/login" or "/auth/refresh")
            return ("auth", _options.Auth, ip);

        if (p is "/account/forgot-password" or "/account/reset-password")
            return ("password-reset", _options.PasswordReset, ip);

        if (p == "/me/cart/checkout")
            return ("checkout", _options.Checkout, userId ?? ip);

        if (p.StartsWith("/products/", StringComparison.Ordinal) && p.EndsWith("/reviews", StringComparison.Ordinal))
            return ("review", _options.Review, userId ?? ip);

        if (p.Contains("/integrations/liqpay/webhook", StringComparison.Ordinal)
            || p.Contains("/integrations/shipping/novaposhta/webhook", StringComparison.Ordinal)
            || p.Contains("/integrations/support/helpdesk/webhook", StringComparison.Ordinal))
            return ("payment_webhook", _options.PaymentWebhook, ip);

        if (p.StartsWith("/admin/payments/", StringComparison.Ordinal)
            && (p.EndsWith("/refund", StringComparison.Ordinal) || p.EndsWith("/sync", StringComparison.Ordinal)))
            return ("payment_admin", _options.PaymentAdmin, userId ?? ip);

        return null;
    }

    private static bool IsAuthEmailEndpoint(PathString path)
    {
        var p = path.Value?.ToLowerInvariant() ?? string.Empty;
        return p is "/auth/register" or "/auth/login";
    }

    private static async Task<string?> TryReadEmailPartitionAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("email", out var email))
                return null;
            var normalized = email.GetString()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }
}
