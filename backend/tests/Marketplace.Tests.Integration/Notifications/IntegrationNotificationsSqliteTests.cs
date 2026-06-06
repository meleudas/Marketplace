using Marketplace.Application.Notifications;
using Marketplace.Domain.Notifications.Enums;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "Notifications")]
public sealed class IntegrationNotificationsSqliteTests
{
    [Fact]
    public async Task InAppNotificationRepository_Insert_List_And_MarkRead_Flow_Works()
    {
        await using var db = await CreateSqliteContextAsync();
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
        Assert.Single(page.Items);
        Assert.Equal("UserOrderStatus", page.Items[0].TemplateKey);
        Assert.True(marked);
        Assert.True(pageAfterRead.Items.Single().IsRead);
    }

    [Fact]
    public async Task InAppNotificationRepository_MarkRead_Enforces_Ownership()
    {
        await using var db = await CreateSqliteContextAsync();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await SeedUserAsync(db, ownerId);
        await SeedUserAsync(db, otherId);
        var repo = new InAppNotificationRepository(db);
        await repo.TryInsertAsync(
            ownerId,
            NotificationKind.System,
            "Система",
            "Тест",
            "{\"templateKey\":\"System\"}",
            null,
            Guid.NewGuid(),
            null,
            null,
            CancellationToken.None);
        var notificationId = (await repo.ListForUserAsync(ownerId, 1, 10, CancellationToken.None)).Items.Single().Id;

        var marked = await repo.MarkReadAsync(otherId, notificationId, CancellationToken.None);

        Assert.False(marked);
    }

    [Fact]
    public async Task InAppNotificationRepository_TryInsert_Is_Idempotent_By_CorrelationId()
    {
        await using var db = await CreateSqliteContextAsync();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        var repo = new InAppNotificationRepository(db);
        var correlationId = Guid.NewGuid();

        var first = await repo.TryInsertAsync(
            userId,
            NotificationKind.Payment,
            "Оплата",
            "Успішно",
            "{\"templateKey\":\"UserPaymentStatus\"}",
            null,
            correlationId,
            null,
            null,
            CancellationToken.None);
        var second = await repo.TryInsertAsync(
            userId,
            NotificationKind.Payment,
            "Оплата",
            "Успішно",
            "{\"templateKey\":\"UserPaymentStatus\"}",
            null,
            correlationId,
            null,
            null,
            CancellationToken.None);
        var page = await repo.ListForUserAsync(userId, 1, 20, CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task PushSubscriptionRepository_Upsert_List_And_Delete_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        var repo = new PushSubscriptionRepository(db);
        const string endpoint = "https://push.example/sub-1";

        await repo.UpsertAsync(
            userId,
            endpoint,
            "p256",
            "auth",
            PushSubscriptionAudienceFlags.UserWebPush | PushSubscriptionAudienceFlags.AdminWebPush,
            "agent",
            CancellationToken.None);
        var forUser = await repo.ListByUserAndAudienceAsync(userId, PushSubscriptionAudienceFlags.UserWebPush, CancellationToken.None);
        var forAdminAudience = await repo.ListByAudienceFlagAsync(PushSubscriptionAudienceFlags.AdminWebPush, CancellationToken.None);
        await repo.DeleteByUserAndEndpointAsync(userId, endpoint, CancellationToken.None);
        var afterDelete = await repo.ListByUserAndAudienceAsync(userId, PushSubscriptionAudienceFlags.UserWebPush, CancellationToken.None);

        Assert.Single(forUser);
        Assert.Single(forAdminAudience);
        Assert.Empty(afterDelete);
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
