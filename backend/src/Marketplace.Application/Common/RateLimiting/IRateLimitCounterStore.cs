namespace Marketplace.Application.Common.RateLimiting;

public sealed record RateLimitAcquireResult(bool Allowed, int RetryAfterSeconds);

public interface IRateLimitCounterStore
{
    Task<RateLimitAcquireResult> TryAcquireAsync(string key, TimeSpan window, int permitLimit, CancellationToken ct = default);
}
