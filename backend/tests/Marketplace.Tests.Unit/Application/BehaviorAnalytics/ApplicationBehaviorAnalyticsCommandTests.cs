using Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Behavior.Services;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Behavior.Entities;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "BehaviorAnalytics")]
public sealed class ApplicationBehaviorAnalyticsCommandTests
{
    [Fact]
    public async Task TrackCatalogInteraction_Deduplicates_In_Short_Window()
    {
        var repo = new InMemoryBehaviorRepository();
        var handler = new TrackCatalogInteractionCommandHandler(
            repo,
            new NoopOutbox(),
            new BehaviorPayloadRedactionService(),
            Options.Create(new BehaviorAnalyticsOptions
            {
                BehaviorTrackingEnabled = true,
                DuplicateWindowSeconds = 60,
                PayloadMaxBytes = 8192,
                SamplingPercent = 100
            }));

        var cmd = new TrackCatalogInteractionCommand(Guid.NewGuid(), "session-1", (short)BehaviorEventType.CatalogClick, "catalog", "{\"x\":1}", true);
        var first = await handler.Handle(cmd, CancellationToken.None);
        var second = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, repo.StoredCount);
    }

    private sealed class InMemoryBehaviorRepository : IBehaviorEventRepository
    {
        private readonly List<BehaviorEvent> _items = [];
        public int StoredCount => _items.Count;

        public Task<BehaviorEvent> AddAsync(BehaviorEvent entity, CancellationToken ct = default)
        {
            _items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<IReadOnlyList<BehaviorEvent>> ListRecentDuplicatesAsync(string eventKey, BehaviorEventType eventType, DateTime sinceUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BehaviorEvent>>(_items.Where(x => x.EventKey == eventKey && x.EventType == eventType && x.OccurredAtUtc >= sinceUtc).ToList());

        public Task<int> CountByTypeAsync(BehaviorEventType eventType, DateTime sinceUtc, DateTime untilUtc, CancellationToken ct = default)
            => Task.FromResult(_items.Count(x => x.EventType == eventType && x.OccurredAtUtc >= sinceUtc && x.OccurredAtUtc <= untilUtc));

        public Task SoftDeleteByUserIdAsync(Guid userId, DateTime deletedAtUtc, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NoopOutbox : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
