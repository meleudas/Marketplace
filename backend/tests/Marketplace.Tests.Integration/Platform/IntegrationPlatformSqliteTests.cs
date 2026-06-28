using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "Platform")]
public sealed class IntegrationPlatformSqliteTests
{
    [Fact]
    public async Task OutboxRepository_Failure_DeadLetter_Requeue_Lifecycle_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var repo = new OutboxRepository(db);

        await repo.AppendAsync("Order", "1", "OrderCreated", "{\"orderId\":1}", CancellationToken.None);
        var pending = await repo.ListPendingAsync(10, DateTime.UtcNow, CancellationToken.None);
        var message = Assert.Single(pending);

        await repo.MarkFailedAsync(message.Id, "temporary", DateTime.UtcNow.AddMinutes(2), CancellationToken.None);
        var failedRow = await db.OutboxMessages.AsNoTracking().FirstAsync(x => x.Id == message.Id);
        Assert.Equal(1, failedRow.Attempts);
        Assert.Equal("temporary", failedRow.LastError);

        await repo.MarkDeadLetterAsync(message.Id, "fatal", "permanent", CancellationToken.None);
        var deadLetterRow = await db.OutboxMessages.AsNoTracking().FirstAsync(x => x.Id == message.Id);
        Assert.NotNull(deadLetterRow.DeadLetteredAtUtc);
        Assert.Equal("permanent", deadLetterRow.DeadLetterCategory);
        Assert.Equal(2, deadLetterRow.Attempts);

        await repo.RequeueDeadLetterAsync(message.Id, CancellationToken.None);
        var requeued = await db.OutboxMessages.AsNoTracking().FirstAsync(x => x.Id == message.Id);
        Assert.Null(requeued.DeadLetteredAtUtc);
        Assert.Null(requeued.DeadLetterReason);
        Assert.Equal(0, requeued.Attempts);
        Assert.NotNull(requeued.NextAttemptAtUtc);
    }

    [Fact]
    public async Task OutboxRepository_ListDeadLetters_Returns_DeadLettered_Messages()
    {
        await using var db = await CreateSqliteContextAsync();
        var repo = new OutboxRepository(db);
        await repo.AppendAsync("Order", "1", "OrderCreated", "{}", CancellationToken.None);
        var message = Assert.Single(await repo.ListPendingAsync(10, DateTime.UtcNow, CancellationToken.None));
        await repo.MarkDeadLetterAsync(message.Id, "fatal", "exhausted", CancellationToken.None);

        var (items, total) = await repo.ListDeadLettersAsync(1, 20, CancellationToken.None);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("exhausted", items[0].DeadLetterCategory);
    }

    [Fact]
    public async Task IntegrationRetryStore_Upsert_Resolve_Lifecycle_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var store = new IntegrationRetryRepository(db);
        await store.UpsertAsync(
            new IntegrationRetryUpsert(
                IntegrationRetryKinds.PaymentSync,
                "Payment",
                "42",
                "{\"paymentId\":42}",
                "sync failed"),
            DateTime.UtcNow,
            CancellationToken.None);

        var due = await store.ListDueAsync(10, DateTime.UtcNow, CancellationToken.None);
        var entry = Assert.Single(due);
        await store.MarkResolvedAsync(entry.Id, CancellationToken.None);

        var after = await store.ListDueAsync(10, DateTime.UtcNow, CancellationToken.None);
        Assert.Empty(after);
    }

    [Fact]
    public async Task HttpIdempotencyStore_Handles_Mismatch_And_Expiry()
    {
        await using var db = await CreateSqliteContextAsync();
        var store = new HttpIdempotencyStore(db);

        var started = await store.TryBeginAsync("scope-1", "key-1", "hash-1", TimeSpan.FromMinutes(10), CancellationToken.None);
        Assert.Equal(HttpIdempotencyBeginState.Started, started.State);

        var mismatch = await store.TryBeginAsync("scope-1", "key-1", "hash-2", TimeSpan.FromMinutes(10), CancellationToken.None);
        Assert.Equal(HttpIdempotencyBeginState.RequestMismatch, mismatch.State);

        var row = await db.HttpIdempotencyRequests.FirstAsync(x => x.Scope == "scope-1" && x.IdempotencyKey == "key-1");
        row.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var restarted = await store.TryBeginAsync("scope-1", "key-1", "hash-3", TimeSpan.FromMinutes(10), CancellationToken.None);
        Assert.Equal(HttpIdempotencyBeginState.Started, restarted.State);
    }

    [Fact]
    public async Task InboxDeduplicator_Duplicate_MarkProcessed_Is_Safe()
    {
        await using var db = await CreateSqliteContextAsync();
        var deduplicator = new InboxDeduplicator(db);
        var messageId = Guid.NewGuid();
        const string consumer = "platform-tests";

        await deduplicator.MarkProcessedAsync(messageId, consumer, "first", CancellationToken.None);
        await deduplicator.MarkProcessedAsync(messageId, consumer, "second", CancellationToken.None);

        var rows = await db.InboxMessages.AsNoTracking().Where(x => x.MessageId == messageId && x.Consumer == consumer).ToListAsync();
        Assert.Single(rows);
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
}
