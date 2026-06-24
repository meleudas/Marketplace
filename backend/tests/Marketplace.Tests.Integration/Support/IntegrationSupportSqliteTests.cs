using Marketplace.Application.Common.Ports;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Support.Commands.AddSupportMessage;
using Marketplace.Application.Support.Commands.AssignSupportTicket;
using Marketplace.Application.Support.Commands.CreateSupportTicket;
using Marketplace.Application.Support.Commands.UpdateTicketStatus;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Support.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Support")]
public sealed class IntegrationSupportSqliteTests
{
    [Fact]
    public async Task Create_AddMessage_Assign_Resolve_Close_Flow_Works()
    {
        await using var db = await CreateSqliteContextAsync();
        var ticketRepo = new SupportTicketRepository(db);
        var messageRepo = new SupportTicketMessageRepository(db);
        var assignmentRepo = new SupportTicketAssignmentRepository(db);
        var eventRepo = new SupportTicketEventRepository(db);
        var externalLinkRepo = new SupportExternalLinkRepository(db);
        var options = Options.Create(new SupportOptions
        {
            Enabled = true,
            HelpdeskSyncEnabled = false,
            CreateRateLimitPerWindow = 20,
            CreateRateLimitWindowMinutes = 60,
            SlaHoursP1 = 4,
            SlaHoursP2 = 24
        });

        var userId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        var outbox = new NoopOutboxWriter();
        var publisher = new SupportHelpdeskOutboxPublisher(outbox, options);

        var create = new CreateSupportTicketCommandHandler(
            ticketRepo,
            eventRepo,
            externalLinkRepo,
            new SupportAntiAbusePolicy(ticketRepo, options),
            new SupportEscalationPolicy(options),
            publisher,
            options);
        var created = await create.Handle(
            new CreateSupportTicketCommand(userId, "Need help", "Initial message", (short)SupportTicketPriority.Normal, null, null, null),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var assign = new AssignSupportTicketCommandHandler(
            ticketRepo,
            assignmentRepo,
            eventRepo,
            new SupportTicketAccessPolicy(ticketRepo),
            publisher,
            options);
        var assigned = await assign.Handle(
            new AssignSupportTicketCommand(staffId, created.Value!.Id, staffId, "Taking case"),
            CancellationToken.None);
        Assert.True(assigned.IsSuccess);
        Assert.Equal((short)SupportTicketStatus.Assigned, assigned.Value!.Status);

        var addMessage = new AddSupportMessageCommandHandler(
            ticketRepo,
            messageRepo,
            eventRepo,
            new SupportTicketAccessPolicy(ticketRepo),
            publisher,
            options);
        var message = await addMessage.Handle(
            new AddSupportMessageCommand(userId, created.Value.Id, "Follow up", false, false),
            CancellationToken.None);
        Assert.True(message.IsSuccess);

        var updateStatus = new UpdateTicketStatusCommandHandler(
            ticketRepo,
            eventRepo,
            new SupportTicketAccessPolicy(ticketRepo),
            new SupportTicketStatePolicy(),
            publisher,
            new StubNotificationScheduler(),
            options);
        var resolved = await updateStatus.Handle(
            new UpdateTicketStatusCommand(staffId, created.Value.Id, (short)SupportTicketStatus.Resolved, "Fixed", true),
            CancellationToken.None);
        Assert.True(resolved.IsSuccess);

        var closed = await updateStatus.Handle(
            new UpdateTicketStatusCommand(staffId, created.Value.Id, (short)SupportTicketStatus.Closed, "Done", true),
            CancellationToken.None);
        Assert.True(closed.IsSuccess);
        Assert.Equal((short)SupportTicketStatus.Closed, closed.Value!.Status);
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

    private sealed class NoopOutboxWriter : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payloadJson, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int limit, DateTime nowUtc, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string error, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListDeadLettersAsync(int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
        public Task<(IReadOnlyList<OutboxMessage> Items, long Total)> ListStuckAsync(DateTime utcNow, int page, int pageSize, CancellationToken ct = default)
            => OutboxWriterFakeDefaults.EmptyListAsync(page, pageSize, ct);
    }

    private sealed class StubNotificationScheduler : IAppNotificationScheduler
    {
        public Task ScheduleAsync(AppNotificationRequest request, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
