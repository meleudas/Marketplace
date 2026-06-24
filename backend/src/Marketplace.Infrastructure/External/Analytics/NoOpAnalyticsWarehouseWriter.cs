using Marketplace.Application.Behavior.Ports;

namespace Marketplace.Infrastructure.External.Analytics;

public sealed class NoOpAnalyticsWarehouseWriter : IAnalyticsWarehouseWriter
{
    public static NoOpAnalyticsWarehouseWriter Instance { get; } = new();

    private NoOpAnalyticsWarehouseWriter()
    {
    }

    public Task EnsureSchemaAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task WriteEventAsync(AnalyticsWarehouseEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task RebuildUserItemSignalsAsync(int lookbackDays, int halfLifeDays, CancellationToken ct = default) => Task.CompletedTask;
    public Task RebuildFunnelDailyAsync(int lookbackDays, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> PingAsync(CancellationToken ct = default) => Task.FromResult(true);
}
