using Marketplace.Application.Reports.Commands.CreateReport;
using Marketplace.Application.Reports.Options;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Entities;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Reports")]
public sealed class ApplicationReportsCommandTests
{
    [Fact]
    public async Task CreateReport_Prevents_Duplicate_In_Cooldown_Window()
    {
        var repo = new InMemoryReportRepository();
        var audit = new InMemoryAuditRepository();
        var options = Options.Create(new ReportsOptions
        {
            PublicCreateEnabled = true,
            DuplicateCooldownMinutes = 30,
            RateLimitPerWindow = 5,
            RateLimitWindowMinutes = 1
        });

        var handler = new CreateReportCommandHandler(repo, audit, options);
        var actor = Guid.NewGuid().ToString();
        var request = new CreateReportCommand(actor, (short)ReportTargetType.Product, "123", (short)ReportReason.Other, "spam", [], (short)ReportPriority.High);

        var first = await handler.Handle(request, CancellationToken.None);
        var second = await handler.Handle(request, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Contains("duplicate", second.Error!, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryReportRepository : IReportRepository
    {
        private readonly Dictionary<long, Report> _items = [];
        private long _nextId = 1;

        public Task<Report?> GetByIdAsync(ReportId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<IReadOnlyList<Report>> ListByReporterAsync(string reporterUserId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Report>>(_items.Values.Where(x => x.ReporterUserId == reporterUserId).ToList());

        public Task<IReadOnlyList<Report>> ListModerationQueueAsync(int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Report>>(_items.Values.Take(limit).ToList());

        public Task<IReadOnlyList<Report>> ListRecentDuplicatesAsync(string reporterUserId, ReportTargetType targetType, string targetId, ReportReason reason, DateTime sinceUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Report>>(_items.Values.Where(x =>
                x.ReporterUserId == reporterUserId &&
                x.TargetType == targetType &&
                x.TargetId == targetId &&
                x.Reason == reason &&
                x.CreatedAt >= sinceUtc).ToList());

        public Task<Report> AddAsync(Report entity, CancellationToken ct = default)
        {
            var saved = Report.Reconstitute(
                ReportId.From(_nextId++),
                entity.ReporterUserId,
                entity.TargetType,
                entity.TargetId,
                entity.Reason,
                entity.Description,
                entity.Images,
                entity.Status,
                entity.ReviewedById,
                entity.ReviewedAt,
                entity.Resolution,
                entity.AssignedModeratorId,
                entity.AssignedAt,
                entity.ClosedById,
                entity.ClosedAt,
                entity.LastActionReason,
                entity.Priority,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.IsDeleted,
                entity.DeletedAt);
            _items[saved.Id.Value] = saved;
            return Task.FromResult(saved);
        }

        public Task UpdateAsync(Report entity, CancellationToken ct = default)
        {
            _items[entity.Id.Value] = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditRepository : IReportActionAuditRepository
    {
        public Task AppendAsync(long reportId, ReportActionType actionType, string actorUserId, string reason, DateTime createdAtUtc, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
