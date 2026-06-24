using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Jobs;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Platform")]
public sealed class OutboxDispatcherJobsTests
{
    [Fact]
    public async Task DispatchPending_Marks_Message_As_Processed_On_Success()
    {
        var message = BuildMessage(attempts: 0);
        var outbox = new SpyOutboxWriter([message]);
        var jobs = new OutboxDispatcherJobs(outbox, new ConfigurableProcessor(), Options.Create(new OutboxOptions()));

        await jobs.DispatchPendingAsync(CancellationToken.None);

        Assert.Contains(message.Id, outbox.ProcessedIds);
        Assert.Empty(outbox.Failed);
        Assert.Empty(outbox.DeadLettered);
    }

    [Fact]
    public async Task DispatchPending_Marks_Permanent_Failures_As_DeadLetter()
    {
        var message = BuildMessage(attempts: 0);
        var outbox = new SpyOutboxWriter([message]);
        var jobs = new OutboxDispatcherJobs(outbox, new ConfigurableProcessor { ThrowPermanent = true }, Options.Create(new OutboxOptions()));

        await jobs.DispatchPendingAsync(CancellationToken.None);

        Assert.Single(outbox.DeadLettered);
        Assert.Equal("permanent", outbox.DeadLettered[0].Category);
    }

    [Fact]
    public async Task DispatchPending_Marks_Transient_Failure_With_Next_Attempt()
    {
        var message = BuildMessage(attempts: 2);
        var outbox = new SpyOutboxWriter([message]);
        var jobs = new OutboxDispatcherJobs(outbox, new ConfigurableProcessor { ThrowTransient = true }, Options.Create(new OutboxOptions()));

        await jobs.DispatchPendingAsync(CancellationToken.None);

        Assert.Single(outbox.Failed);
        Assert.True(outbox.Failed[0].NextAttemptAtUtc > DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task DispatchPending_Marks_As_Exhausted_When_Attempts_Exceeded()
    {
        var message = BuildMessage(attempts: 9);
        var outbox = new SpyOutboxWriter([message]);
        var jobs = new OutboxDispatcherJobs(outbox, new ConfigurableProcessor { ThrowTransient = true }, Options.Create(new OutboxOptions()));

        await jobs.DispatchPendingAsync(CancellationToken.None);

        Assert.Single(outbox.DeadLettered);
        Assert.Equal("exhausted", outbox.DeadLettered[0].Category);
        Assert.Empty(outbox.Failed);
    }

    private static OutboxMessage BuildMessage(int attempts)
        => new(
            Guid.NewGuid(),
            "Order",
            "1",
            "OrderStatusChanged",
            "{\"orderId\":1}",
            DateTime.UtcNow,
            null,
            attempts,
            null,
            DateTime.UtcNow,
            null,
            null,
            null);

    private sealed class ConfigurableProcessor : IOutboxEventProcessor
    {
        public bool ThrowPermanent { get; set; }
        public bool ThrowTransient { get; set; }

        public Task ProcessAsync(OutboxMessage message, CancellationToken ct = default)
        {
            if (ThrowPermanent)
                throw new PermanentOutboxException("unsupported payload");
            if (ThrowTransient)
                throw new InvalidOperationException("temporary failure");
            return Task.CompletedTask;
        }
    }

    private sealed class SpyOutboxWriter : IOutboxWriter
    {
        private readonly IReadOnlyList<OutboxMessage> _pending;

        public SpyOutboxWriter(IReadOnlyList<OutboxMessage> pending)
        {
            _pending = pending;
        }

        public List<Guid> ProcessedIds { get; } = [];
        public List<(Guid Id, DateTime NextAttemptAtUtc)> Failed { get; } = [];
        public List<(Guid Id, string Category)> DeadLettered { get; } = [];

        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult(_pending);

        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
        {
            ProcessedIds.Add(id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default)
        {
            Failed.Add((id, nextAttemptAtUtc));
            return Task.CompletedTask;
        }

        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default)
        {
            DeadLettered.Add((id, category));
            return Task.CompletedTask;
        }

        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(((IReadOnlyList<OutboxMessage>)Array.Empty<OutboxMessage>(), 0L));

        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(((IReadOnlyList<OutboxMessage>)Array.Empty<OutboxMessage>(), 0L));
    }
}
