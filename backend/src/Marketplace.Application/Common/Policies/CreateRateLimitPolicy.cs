using Marketplace.Application.Common.RateLimiting;

namespace Marketplace.Application.Common.Policies;

public sealed class CreateRateLimitPolicy
{
    private readonly IRateLimitCounterStore _store;

    public CreateRateLimitPolicy(IRateLimitCounterStore store) => _store = store;

    public async Task<(bool Allowed, string? Reason, int RetryAfterSeconds)> EvaluateAsync(
        string key,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default)
    {
        var limit = Math.Max(1, permitLimit);
        var result = await _store.TryAcquireAsync(key, window, limit, ct);
        if (!result.Allowed)
            return (false, "rate exceeded", result.RetryAfterSeconds);
        return (true, null, 0);
    }
}
