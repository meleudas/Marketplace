using Marketplace.Domain.Notifications.Enums;
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
public sealed class InAppPushPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public InAppPushPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task InAppNotification_With_Postgres_Persists_And_Supports_MarkRead()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        var repo = new InAppNotificationRepository(db);

        var inserted = await repo.TryInsertAsync(
            userId,
            NotificationKind.Order,
            "Статус",
            "Замовлення відправлене",
            "{\"templateKey\":\"UserOrderStatus\",\"orderId\":1}",
            "/orders/1",
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(7),
            null,
            CancellationToken.None);

        var page = await repo.ListForUserAsync(userId, 1, 20, CancellationToken.None);
        var marked = await repo.MarkReadAsync(userId, page.Items.Single().Id, CancellationToken.None);
        var pageAfterRead = await repo.ListForUserAsync(userId, 1, 20, CancellationToken.None);

        Assert.True(inserted);
        Assert.True(marked);
        Assert.True(pageAfterRead.Items.Single().IsRead);
    }

    private static async Task SeedUserAsync(ApplicationDbContext db, Guid userId)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"user-{userId:N}",
            NormalizedUserName = $"USER-{userId:N}",
            Email = $"{userId:N}@example.test",
            NormalizedEmail = $"{userId:N}@EXAMPLE.TEST",
            EmailConfirmed = true
        });
        await db.SaveChangesAsync();
    }
}
