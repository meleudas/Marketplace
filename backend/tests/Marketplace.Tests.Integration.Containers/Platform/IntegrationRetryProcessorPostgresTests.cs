using System.Text.Json;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Platform;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Platform")]
[Trait("Layer", "IntegrationContainers")]
public sealed class IntegrationRetryProcessorPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public IntegrationRetryProcessorPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Upsert_Process_And_Resolve_Notification_Retry_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Marketplace.Infrastructure.Persistence.ApplicationDbContext>();
        var store = new IntegrationRetryRepository(db);
        var processor = scope.ServiceProvider.GetRequiredService<IntegrationRetryProcessor>();

        var correlationId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            templateKey = AppNotificationTemplateKeys.UserOrderStatus,
            correlationId,
            channels = 1,
            audience = 1,
            targetUserId = Guid.NewGuid(),
            payloadJson = "{\"orderId\":1}"
        });

        await store.UpsertAsync(
            new IntegrationRetryUpsert(
                IntegrationRetryKinds.NotificationDispatch,
                "AppNotification",
                $"{correlationId:N}:Email",
                payload,
                "smtp down"),
            DateTime.UtcNow,
            CancellationToken.None);

        var due = await store.ListDueAsync(10, DateTime.UtcNow, CancellationToken.None);
        var entry = Assert.Single(due, x => x.AggregateId == $"{correlationId:N}:Email");

        var processed = await processor.TryProcessAsync(entry, CancellationToken.None);
        Assert.True(processed);
        await store.MarkResolvedAsync(entry.Id, CancellationToken.None);

        var after = await store.ListDueAsync(10, DateTime.UtcNow, CancellationToken.None);
        Assert.DoesNotContain(after, x => x.Id == entry.Id);
    }
}
