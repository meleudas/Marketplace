using Marketplace.Application.Notifications.Commands.MarkNotificationRead;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Notifications.Queries.GetMyNotifications;
using Marketplace.Domain.Notifications.Enums;

namespace Marketplace.Tests;

[Trait("Suite", "Notifications")]
public sealed class ApplicationNotificationHandlersTests
{
    [Fact]
    public async Task GetMyNotifications_Returns_Failure_When_Page_Invalid()
    {
        var handler = new GetMyNotificationsQueryHandler(new InMemoryInAppNotificationRepository());

        var result = await handler.Handle(new GetMyNotificationsQuery(Guid.NewGuid(), 0, 20), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Page must be at least 1", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMyNotifications_Returns_Failure_When_PageSize_Out_Of_Range()
    {
        var handler = new GetMyNotificationsQueryHandler(new InMemoryInAppNotificationRepository());

        var result = await handler.Handle(new GetMyNotificationsQuery(Guid.NewGuid(), 1, 101), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("PageSize", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMyNotifications_Returns_Paged_Items_For_User()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var repo = new InMemoryInAppNotificationRepository();
        await repo.TryInsertAsync(userId, NotificationKind.Order, "T1", "M1", "{}", null, Guid.NewGuid(), null, null, CancellationToken.None);
        await repo.TryInsertAsync(userId, NotificationKind.System, "T2", "M2", "{}", null, Guid.NewGuid(), null, null, CancellationToken.None);
        await repo.TryInsertAsync(otherUserId, NotificationKind.Payment, "T3", "M3", "{}", null, Guid.NewGuid(), null, null, CancellationToken.None);
        var handler = new GetMyNotificationsQueryHandler(repo);

        var result = await handler.Handle(new GetMyNotificationsQuery(userId, 1, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Total);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, x => Assert.NotEqual("T3", x.Title));
    }

    [Fact]
    public async Task MarkNotificationRead_Returns_NotFound_For_Alien_Notification()
    {
        var owner = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var repo = new InMemoryInAppNotificationRepository();
        await repo.TryInsertAsync(owner, NotificationKind.Order, "T1", "M1", "{}", null, Guid.NewGuid(), null, null, CancellationToken.None);
        var notificationId = repo.Items.Single().Id;
        var handler = new MarkNotificationReadCommandHandler(repo);

        var result = await handler.Handle(new MarkNotificationReadCommand(actor, notificationId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkNotificationRead_Marks_Notification_For_Owner()
    {
        var owner = Guid.NewGuid();
        var repo = new InMemoryInAppNotificationRepository();
        await repo.TryInsertAsync(owner, NotificationKind.Order, "T1", "M1", "{}", null, Guid.NewGuid(), null, null, CancellationToken.None);
        var notificationId = repo.Items.Single().Id;
        var handler = new MarkNotificationReadCommandHandler(repo);

        var result = await handler.Handle(new MarkNotificationReadCommand(owner, notificationId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.Items.Single().IsRead);
        Assert.NotNull(repo.Items.Single().ReadAt);
    }

    private sealed class InMemoryInAppNotificationRepository : IInAppNotificationRepository
    {
        private long _nextId = 1;
        public List<Row> Items { get; } = [];

        public Task<bool> TryInsertAsync(
            Guid userId,
            NotificationKind kind,
            string title,
            string message,
            string dataJson,
            string? actionUrl,
            Guid? correlationId,
            DateTime? expiresAtUtc,
            string? rawPayload,
            CancellationToken ct = default)
        {
            if (correlationId.HasValue && Items.Any(x => x.UserId == userId && x.CorrelationId == correlationId))
                return Task.FromResult(false);

            Items.Add(new Row
            {
                Id = _nextId++,
                UserId = userId,
                Kind = kind,
                Title = title,
                Message = message,
                DataJson = dataJson,
                ActionUrl = actionUrl,
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAtUtc
            });
            return Task.FromResult(true);
        }

        public Task<PagedInAppNotificationsDto> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            var source = Items
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToList();
            var items = source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InAppNotificationListItemDto(
                    x.Id,
                    string.Empty,
                    x.CorrelationId,
                    x.Kind,
                    x.Title,
                    x.Message,
                    x.ActionUrl,
                    x.IsRead,
                    x.ReadAt,
                    x.CreatedAt,
                    x.DataJson))
                .ToList();

            return Task.FromResult(new PagedInAppNotificationsDto(items, source.Count, page, pageSize));
        }

        public Task<bool> MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default)
        {
            var row = Items.FirstOrDefault(x => x.Id == notificationId && x.UserId == userId);
            if (row is null)
                return Task.FromResult(false);

            row.IsRead = true;
            row.ReadAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<int> DeleteExpiredBeforeAsync(DateTime utcNow, CancellationToken ct = default)
        {
            var removed = Items.RemoveAll(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value < utcNow);
            return Task.FromResult(removed);
        }

        public sealed class Row
        {
            public long Id { get; set; }
            public Guid UserId { get; set; }
            public NotificationKind Kind { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string DataJson { get; set; } = "{}";
            public string? ActionUrl { get; set; }
            public Guid? CorrelationId { get; set; }
            public bool IsRead { get; set; }
            public DateTime? ReadAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }
    }
}
