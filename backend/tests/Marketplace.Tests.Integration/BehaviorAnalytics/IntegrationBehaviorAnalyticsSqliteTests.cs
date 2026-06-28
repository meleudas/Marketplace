using Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Behavior.Queries.GetBehaviorSummary;
using Marketplace.Application.Behavior.Services;
using Marketplace.Application.Common.Ports;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "BehaviorAnalytics")]
public sealed class IntegrationBehaviorAnalyticsSqliteTests
{
    [Fact]
    public async Task Ingest_Then_Read_Summary_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var handler = new TrackCatalogInteractionCommandHandler(
            new BehaviorEventRepository(db),
            new NoopOutbox(),
            new BehaviorPayloadRedactionService(),
            Options.Create(new BehaviorAnalyticsOptions
            {
                BehaviorTrackingEnabled = true,
                DuplicateWindowSeconds = 1,
                PayloadMaxBytes = 8192,
                SamplingPercent = 100
            }));

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var track = await handler.Handle(new TrackCatalogInteractionCommand(Guid.NewGuid(), "sess", (short)BehaviorEventType.ProductView, "catalog", "{\"ok\":true}", true), CancellationToken.None);
        Assert.True(track.IsSuccess);

        var query = new GetBehaviorSummaryQueryHandler(new BehaviorEventRepository(db));
        var summary = await query.Handle(new GetBehaviorSummaryQuery(now, now), CancellationToken.None);
        Assert.True(summary.IsSuccess);
        Assert.True(summary.Value!.ProductViews >= 1);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class NoopOutbox : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
    }
}
