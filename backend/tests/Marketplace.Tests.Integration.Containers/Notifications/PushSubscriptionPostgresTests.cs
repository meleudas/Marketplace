using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Notifications;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Notifications")]
[Trait("Layer", "IntegrationContainers")]
public sealed class PushSubscriptionPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public PushSubscriptionPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Upsert_And_List_Push_Subscription_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);

        var repo = scope.ServiceProvider.GetRequiredService<IPushSubscriptionRepository>();
        const string endpoint = "https://push.example.test/subscription/1";

        await repo.UpsertAsync(
            userId,
            endpoint,
            "p256dh-key",
            "auth-secret",
            PushSubscriptionAudienceFlags.UserWebPush,
            userAgent: null,
            CancellationToken.None);

        var listed = await repo.ListByUserAndAudienceAsync(userId, PushSubscriptionAudienceFlags.UserWebPush, CancellationToken.None);
        var subscription = Assert.Single(listed);
        Assert.Equal(endpoint, subscription.Endpoint);
    }

    private static async Task SeedUserAsync(ApplicationDbContext db, Guid userId)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"push-{userId:N}",
            NormalizedUserName = $"PUSH-{userId:N}",
            Email = $"{userId:N}@push.test",
            NormalizedEmail = $"{userId:N}@PUSH.TEST",
            EmailConfirmed = true
        });
        await db.SaveChangesAsync();
    }
}
