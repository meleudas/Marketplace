using Marketplace.Application.Reports.Commands.CreateReport;
using Marketplace.Application.Reports.Options;
using Marketplace.Application.Reports.Queries.GetModerationQueue;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Reports")]
public sealed class IntegrationReportsSqliteTests
{
    [Fact]
    public async Task Create_And_List_Queue_Works_With_Sqlite()
    {
        await using var db = await CreateSqliteContextAsync();
        var reportRepo = new ReportRepository(db);
        var auditRepo = new ReportActionAuditRepository(db);

        var create = new CreateReportCommandHandler(
            reportRepo,
            auditRepo,
            Options.Create(new ReportsOptions
            {
                PublicCreateEnabled = true,
                DuplicateCooldownMinutes = 1,
                RateLimitPerWindow = 10,
                RateLimitWindowMinutes = 1
            }));

        var userId = Guid.NewGuid().ToString();
        var created = await create.Handle(
            new CreateReportCommand(userId, 0, "p-1", 0, "content", [], 2),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var queue = new GetModerationQueueQueryHandler(reportRepo);
        var queueResult = await queue.Handle(new GetModerationQueueQuery(10), CancellationToken.None);
        Assert.True(queueResult.IsSuccess);
        Assert.Single(queueResult.Value!);
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
