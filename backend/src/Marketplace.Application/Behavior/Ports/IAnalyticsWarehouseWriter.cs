namespace Marketplace.Application.Behavior.Ports;

public sealed record AnalyticsWarehouseEvent(
    Guid EventId,
    string EventType,
    DateTime OccurredAtUtc,
    Guid? UserId,
    string SessionId,
    long? ProductId,
    string? Query,
    string Source,
    short SchemaVersion,
    string PayloadJson,
    DateTime CreatedAtUtc);

public interface IAnalyticsWarehouseWriter
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task WriteEventAsync(AnalyticsWarehouseEvent evt, CancellationToken ct = default);
    Task RebuildUserItemSignalsAsync(int lookbackDays, int halfLifeDays, CancellationToken ct = default);
    Task RebuildFunnelDailyAsync(int lookbackDays, CancellationToken ct = default);
    Task<bool> PingAsync(CancellationToken ct = default);
}
