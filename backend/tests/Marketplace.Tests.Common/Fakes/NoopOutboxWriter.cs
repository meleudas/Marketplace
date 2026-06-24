using Marketplace.Application.Common.Ports;

namespace Marketplace.Tests.Common.Fakes;

public sealed class NoopOutboxWriter : IOutboxWriter
{
    public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

    public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

    public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
        => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);

    public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
        => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
}
