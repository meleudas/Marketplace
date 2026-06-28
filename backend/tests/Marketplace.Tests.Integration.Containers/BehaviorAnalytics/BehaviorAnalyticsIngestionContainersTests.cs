using Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Behavior.Queries.GetBehaviorSummary;
using Marketplace.Application.Behavior.Services;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.BehaviorAnalytics;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "BehaviorAnalytics")]
[Trait("Layer", "IntegrationContainers")]
public sealed class BehaviorAnalyticsIngestionContainersTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public BehaviorAnalyticsIngestionContainersTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ingest_Then_Read_Summary_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var outbox = new OutboxRepository(db);
        var handler = new TrackCatalogInteractionCommandHandler(
            new BehaviorEventRepository(db),
            outbox,
            new BehaviorPayloadRedactionService(),
            Options.Create(new BehaviorAnalyticsOptions
            {
                BehaviorTrackingEnabled = true,
                DuplicateWindowSeconds = 1,
                PayloadMaxBytes = 8192,
                SamplingPercent = 100
            }));

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var track = await handler.Handle(
            new TrackCatalogInteractionCommand(Guid.NewGuid(), "pg-sess", (short)BehaviorEventType.ProductView, "catalog", "{\"ok\":true}", true),
            CancellationToken.None);
        Assert.True(track.IsSuccess);

        var query = new GetBehaviorSummaryQueryHandler(new BehaviorEventRepository(db));
        var summary = await query.Handle(new GetBehaviorSummaryQuery(now, now), CancellationToken.None);
        Assert.True(summary.IsSuccess);
        Assert.True(summary.Value!.ProductViews >= 1);
    }
}
