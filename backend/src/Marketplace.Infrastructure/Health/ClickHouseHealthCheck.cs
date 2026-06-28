using Marketplace.Application.Behavior.Ports;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marketplace.Infrastructure.Health;

public sealed class ClickHouseHealthCheck : IHealthCheck
{
    private readonly IAnalyticsWarehouseWriter _warehouseWriter;

    public ClickHouseHealthCheck(IAnalyticsWarehouseWriter warehouseWriter) => _warehouseWriter = warehouseWriter;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await _warehouseWriter.PingAsync(ct)
                ? HealthCheckResult.Healthy("ClickHouse is reachable")
                : HealthCheckResult.Degraded("ClickHouse is not reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("ClickHouse check failed", ex);
        }
    }
}
