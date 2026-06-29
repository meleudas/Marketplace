using Marketplace.Application.Common.Policies;
using Marketplace.Application.Payments.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Payments.Policies;

public sealed class PaymentWebhookAntiAbusePolicy
{
    private readonly CreateRateLimitPolicy _rateLimit;
    private readonly PaymentWebhookAntiAbuseOptions _options;

    public PaymentWebhookAntiAbusePolicy(CreateRateLimitPolicy rateLimit, IOptions<PaymentWebhookAntiAbuseOptions> options)
    {
        _rateLimit = rateLimit;
        _options = options.Value;
    }

    public Task<(bool Allowed, string? Reason, int RetryAfterSeconds)> EvaluateClientIpAsync(
        string? clientIp,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
            return Task.FromResult<(bool, string?, int)>((true, null, 0));

        var window = TimeSpan.FromMinutes(Math.Max(1, _options.FailedSignatureWindowMinutes));
        var key = $"payment-webhook-fail:{clientIp}";
        return _rateLimit.EvaluateAsync(key, _options.FailedSignaturePerIpPerWindow, window, ct);
    }

    public Task RecordFailedSignatureAsync(string? clientIp, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
            return Task.CompletedTask;

        var window = TimeSpan.FromMinutes(Math.Max(1, _options.FailedSignatureWindowMinutes));
        var key = $"payment-webhook-fail:{clientIp}";
        return _rateLimit.EvaluateAsync(key, _options.FailedSignaturePerIpPerWindow, window, ct);
    }
}
