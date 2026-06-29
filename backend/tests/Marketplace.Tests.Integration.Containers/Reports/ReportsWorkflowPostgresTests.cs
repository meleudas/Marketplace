using Marketplace.Application.Reports.Commands.CreateReport;
using Marketplace.Application.Reports.Commands.ResolveReportCase;
using Marketplace.Application.Reports.Options;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.Reports;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Reports")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ReportsWorkflowPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ReportsWorkflowPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_And_Resolve_Report_On_Postgres()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Marketplace.Infrastructure.Persistence.ApplicationDbContext>();
        var reportRepo = new ReportRepository(db);
        var auditRepo = new ReportActionAuditRepository(db);

        var create = new CreateReportCommandHandler(reportRepo, auditRepo, Options.Create(new ReportsOptions
        {
            PublicCreateEnabled = true,
            DuplicateCooldownMinutes = 1,
            RateLimitPerWindow = 10,
            RateLimitWindowMinutes = 1
        }));
        var resolve = new ResolveReportCaseCommandHandler(reportRepo, auditRepo);

        var reporterId = Guid.NewGuid().ToString();
        var moderatorId = Guid.NewGuid().ToString();

        var created = await create.Handle(
            new CreateReportCommand(reporterId, (short)ReportTargetType.Product, "p-1", (short)ReportReason.Fraud, "content", [], (short)ReportPriority.High),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var report = await reportRepo.GetByIdAsync(ReportId.From(created.Value!.Id), CancellationToken.None);
        Assert.NotNull(report);
        var review = report!.StartReview(moderatorId, "review", DateTime.UtcNow);
        Assert.True(review.IsSuccess);
        await reportRepo.UpdateAsync(report, CancellationToken.None);

        var resolved = await resolve.Handle(
            new ResolveReportCaseCommand(created.Value!.Id, moderatorId, "handled", true),
            CancellationToken.None);
        Assert.True(resolved.IsSuccess);
        Assert.Equal((short)ReportStatus.Closed, resolved.Value!.Status);
    }
}
