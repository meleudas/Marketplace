using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reports.Commands.ResolveReportCase;

public sealed record ResolveReportCaseCommand(long ReportId, string ActorUserId, string Resolution, bool CloseImmediately) : IRequest<Result<ReportDto>>;

public sealed class ResolveReportCaseCommandHandler : IRequestHandler<ResolveReportCaseCommand, Result<ReportDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly IReportActionAuditRepository _auditRepository;

    public ResolveReportCaseCommandHandler(IReportRepository reportRepository, IReportActionAuditRepository auditRepository)
    {
        _reportRepository = reportRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Result<ReportDto>> Handle(ResolveReportCaseCommand request, CancellationToken ct)
    {
        var report = await _reportRepository.GetByIdAsync(ReportId.From(request.ReportId), ct);
        if (report is null)
            return Result<ReportDto>.Failure("not found: report not found");

        var now = DateTime.UtcNow;
        var resolve = report.Resolve(request.ActorUserId, request.Resolution, now);
        if (resolve.IsFailure)
            return Result<ReportDto>.Failure(resolve.Error!);

        await _auditRepository.AppendAsync(report.Id.Value, ReportActionType.Resolved, request.ActorUserId, request.Resolution, now, ct);

        if (request.CloseImmediately)
        {
            var close = report.Close(request.ActorUserId, "closed after resolution", now);
            if (close.IsFailure)
                return Result<ReportDto>.Failure(close.Error!);
            await _auditRepository.AppendAsync(report.Id.Value, ReportActionType.Closed, request.ActorUserId, "closed after resolution", now, ct);
        }

        await _reportRepository.UpdateAsync(report, ct);
        return Result<ReportDto>.Success(report.ToDto());
    }
}
