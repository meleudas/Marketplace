using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reports.Commands.EscalateReportCase;

public sealed record EscalateReportCaseCommand(long ReportId, string ActorUserId, string Reason) : IRequest<Result<ReportDto>>;

public sealed class EscalateReportCaseCommandHandler : IRequestHandler<EscalateReportCaseCommand, Result<ReportDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly IReportActionAuditRepository _auditRepository;

    public EscalateReportCaseCommandHandler(IReportRepository reportRepository, IReportActionAuditRepository auditRepository)
    {
        _reportRepository = reportRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Result<ReportDto>> Handle(EscalateReportCaseCommand request, CancellationToken ct)
    {
        var report = await _reportRepository.GetByIdAsync(ReportId.From(request.ReportId), ct);
        if (report is null)
            return Result<ReportDto>.Failure("not found: report not found");

        var now = DateTime.UtcNow;
        var result = report.Escalate(request.ActorUserId, request.Reason, now);
        if (result.IsFailure)
            return Result<ReportDto>.Failure(result.Error!);

        await _reportRepository.UpdateAsync(report, ct);
        await _auditRepository.AppendAsync(report.Id.Value, ReportActionType.Escalated, request.ActorUserId, request.Reason, now, ct);
        return Result<ReportDto>.Success(report.ToDto());
    }
}
