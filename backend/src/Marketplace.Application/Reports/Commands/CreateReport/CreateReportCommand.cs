using FluentValidation;
using Marketplace.Application.Reports.DTOs;
using Marketplace.Application.Reports.Options;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Entities;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Reports.Commands.CreateReport;

public sealed record CreateReportCommand(
    string ActorUserId,
    short TargetType,
    string TargetId,
    short Reason,
    string Description,
    string[] Images,
    short Priority) : IRequest<Result<ReportDto>>;

public sealed class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Images).Must(x => x.Length <= 10);
    }
}

public sealed class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, Result<ReportDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly IReportActionAuditRepository _auditRepository;
    private readonly ReportsOptions _options;

    public CreateReportCommandHandler(
        IReportRepository reportRepository,
        IReportActionAuditRepository auditRepository,
        IOptions<ReportsOptions> options)
    {
        _reportRepository = reportRepository;
        _auditRepository = auditRepository;
        _options = options.Value;
    }

    public async Task<Result<ReportDto>> Handle(CreateReportCommand request, CancellationToken ct)
    {
        if (!_options.PublicCreateEnabled)
            return Result<ReportDto>.Failure("conflict: reports public create is disabled");

        var now = DateTime.UtcNow;
        var rateWindowStart = now.AddMinutes(-Math.Max(1, _options.RateLimitWindowMinutes));
        var recentByActor = await _reportRepository.ListRecentDuplicatesAsync(
            request.ActorUserId,
            (ReportTargetType)request.TargetType,
            request.TargetId,
            (ReportReason)request.Reason,
            rateWindowStart,
            ct);

        if (recentByActor.Count >= Math.Max(1, _options.RateLimitPerWindow))
            return Result<ReportDto>.Failure("conflict: reports rate limit exceeded");

        var dedupWindowStart = now.AddMinutes(-Math.Max(1, _options.DuplicateCooldownMinutes));
        var duplicates = await _reportRepository.ListRecentDuplicatesAsync(
            request.ActorUserId,
            (ReportTargetType)request.TargetType,
            request.TargetId,
            (ReportReason)request.Reason,
            dedupWindowStart,
            ct);
        if (duplicates.Count > 0)
            return Result<ReportDto>.Failure("conflict: duplicate report in cooldown window");

        var report = Report.Create(
            request.ActorUserId,
            (ReportTargetType)request.TargetType,
            request.TargetId,
            (ReportReason)request.Reason,
            request.Description,
            new JsonBlob(System.Text.Json.JsonSerializer.Serialize(request.Images)),
            (ReportPriority)request.Priority,
            now);

        var saved = await _reportRepository.AddAsync(report, ct);
        await _auditRepository.AppendAsync(saved.Id.Value, ReportActionType.Created, request.ActorUserId, "created", now, ct);
        return Result<ReportDto>.Success(saved.ToDto());
    }
}
