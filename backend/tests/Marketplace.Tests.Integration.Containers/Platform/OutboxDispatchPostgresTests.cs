using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.Platform;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Platform")]
[Trait("Layer", "IntegrationContainers")]
public sealed class OutboxDispatchPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public OutboxDispatchPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DispatchPending_With_Postgres_Marks_Message_Processed()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var outbox = new OutboxRepository(db);

        const string aggregateId = "outbox-dispatch-test-42";
        await outbox.AppendAsync("Order", aggregateId, "OrderStatusChanged", "{\"orderId\":42}", CancellationToken.None);

        var jobs = new OutboxDispatcherJobs(outbox, new NoOpOutboxEventProcessor(), Options.Create(new OutboxOptions()));
        await jobs.DispatchPendingAsync(CancellationToken.None);

        var row = await db.OutboxMessages.AsNoTracking()
            .SingleAsync(x => x.AggregateType == "Order" && x.AggregateId == aggregateId);
        Assert.NotNull(row.ProcessedAtUtc);
    }

    private sealed class NoOpOutboxEventProcessor : IOutboxEventProcessor
    {
        public Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
