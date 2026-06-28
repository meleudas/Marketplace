using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marketplace.Infrastructure.Health;

public sealed class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;

    public ElasticsearchHealthCheck(ElasticsearchClient client) => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Cluster.HealthAsync(ct);
            if (response.IsValidResponse)
                return HealthCheckResult.Healthy($"Elasticsearch status: {response.Status}");

            return HealthCheckResult.Degraded(response.ElasticsearchServerError?.Error?.Reason ?? "Elasticsearch unhealthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Elasticsearch check failed", ex);
        }
    }
}
