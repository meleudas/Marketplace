using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reports.Enums;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reports.Commands.AssignReportCase;

public sealed record AssignReportCaseCommand(long ReportId, string ActorUserId, string ModeratorUserId, string Reason) : IRequest<Result<ReportDto>>;

public sealed class AssignReportCaseCommandHandler : IRequestHandler<AssignReportCaseCommand, Result<ReportDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly IReportActionAuditRepository _auditRepository;

    public AssignReportCaseCommandHandler(IReportRepository reportRepository, IReportActionAuditRepository auditRepository)
    {
        _reportRepository = reportRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Result<ReportDto>> Handle(AssignReportCaseCommand request, CancellationToken ct)
    {
        var report = await _reportRepository.GetByIdAsync(ReportId.From(request.ReportId), ct);
        if (report is null)
            return Result<ReportDto>.Failure("not found: report not found");

        var now = DateTime.UtcNow;
        var result = report.Assign(request.ModeratorUserId, request.ActorUserId, request.Reason, now);
        if (result.IsFailure)
            return Result<ReportDto>.Failure(result.Error!);

        await _reportRepository.UpdateAsync(report, ct);
        await _auditRepository.AppendAsync(report.Id.Value, ReportActionType.Assigned, request.ActorUserId, request.Reason, now, ct);
        return Result<ReportDto>.Success(report.ToDto());
    }
}
