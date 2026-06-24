using Marketplace.Infrastructure.External.Recommendations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marketplace.Infrastructure.Health;

public sealed class RecommendationModelHealthCheck : IHealthCheck
{
    private readonly RecommendationModelLoader _loader;

    public RecommendationModelHealthCheck(RecommendationModelLoader loader)
    {
        _loader = loader;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var model = await _loader.GetActiveAsync(cancellationToken);
        return model is null
            ? HealthCheckResult.Degraded("No active ML recommendation model loaded")
            : HealthCheckResult.Healthy($"ML recommendation model loaded: {model.Version}");
    }
}
