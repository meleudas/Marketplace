using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Health;

public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly IObjectStorage _storage;
    private readonly StorageOptions _options;

    public StorageHealthCheck(IObjectStorage storage, IOptions<StorageOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return HealthCheckResult.Healthy("Object storage is disabled");

        try
        {
            await _storage.EnsureBucketExistsAsync(ct);
            return HealthCheckResult.Healthy("Object storage bucket is reachable");
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrWhiteSpace(ex.Message)
                ? "Object storage check failed"
                : $"Object storage check failed: {ex.Message}";
            return HealthCheckResult.Degraded(message, ex);
        }
    }
}
