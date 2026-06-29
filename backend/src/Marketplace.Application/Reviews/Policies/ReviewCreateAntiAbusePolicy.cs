using Marketplace.Application.Common.Policies;
using Marketplace.Application.Reviews.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Reviews.Policies;

public sealed class ReviewCreateAntiAbusePolicy
{
    private readonly CreateRateLimitPolicy _rateLimit;
    private readonly ReviewsAntiAbuseOptions _options;

    public ReviewCreateAntiAbusePolicy(CreateRateLimitPolicy rateLimit, IOptions<ReviewsAntiAbuseOptions> options)
    {
        _rateLimit = rateLimit;
        _options = options.Value;
    }

    public Task<(bool Allowed, string? Reason, int RetryAfterSeconds)> EvaluateCreateAsync(
        Guid userId,
        long productId,
        CancellationToken ct = default)
    {
        var window = TimeSpan.FromMinutes(Math.Max(1, _options.CreateWindowMinutes));
        var key = $"review-create:{userId:N}:{productId}";
        return _rateLimit.EvaluateAsync(key, _options.CreatePerUserProductPerDay, window, ct);
    }
}
