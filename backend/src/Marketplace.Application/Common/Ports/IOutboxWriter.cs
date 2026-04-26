namespace Marketplace.Application.Common.Ports;

public interface IOutboxWriter
{
    Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default);
}
