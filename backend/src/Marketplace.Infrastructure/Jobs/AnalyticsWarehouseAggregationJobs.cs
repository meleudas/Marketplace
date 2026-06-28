using Marketplace.Application.Behavior.Ports;
using Marketplace.Infrastructure.External.Analytics;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class AnalyticsWarehouseAggregationJobs
{
    private readonly IAnalyticsWarehouseWriter _warehouseWriter;
    private readonly ClickHouseOptions _options;

    public AnalyticsWarehouseAggregationJobs(
        IAnalyticsWarehouseWriter warehouseWriter,
        IOptions<ClickHouseOptions> options)
    {
        _warehouseWriter = warehouseWriter;
        _options = options.Value;
    }

    public async Task RebuildUserItemSignalsAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await _warehouseWriter.RebuildUserItemSignalsAsync(
            _options.SignalLookbackDays,
            _options.SignalHalfLifeDays,
            ct);
    }

    public async Task RebuildFunnelDailyAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await _warehouseWriter.RebuildFunnelDailyAsync(_options.FunnelLookbackDays, ct);
    }
}
