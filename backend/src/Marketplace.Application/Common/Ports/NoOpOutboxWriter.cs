namespace Marketplace.Application.Common.Ports;

public sealed class NoOpOutboxWriter : IOutboxWriter
{
    public static NoOpOutboxWriter Instance { get; } = new();

    private NoOpOutboxWriter()
    {
    }

    public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

    public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<OutboxMessage>, long)>(([], 0));
    public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<OutboxMessage>, long)>(([], 0));
}
