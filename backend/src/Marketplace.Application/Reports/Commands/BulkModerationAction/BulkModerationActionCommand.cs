using Marketplace.Application.Reports.Commands.EscalateReportCase;
using Marketplace.Application.Reports.Commands.ResolveReportCase;
using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reports.Commands.BulkModerationAction;

public sealed record BulkModerationActionCommand(
    string ActorUserId,
    string Action,
    IReadOnlyList<long> ReportIds,
    string Reason) : IRequest<Result<IReadOnlyList<ReportDto>>>;

public sealed class BulkModerationActionCommandHandler : IRequestHandler<BulkModerationActionCommand, Result<IReadOnlyList<ReportDto>>>
{
    private readonly ISender _sender;

    public BulkModerationActionCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<IReadOnlyList<ReportDto>>> Handle(BulkModerationActionCommand request, CancellationToken ct)
    {
        var items = new List<ReportDto>();
        foreach (var reportId in request.ReportIds.Distinct())
        {
            Result<ReportDto> result = request.Action.ToLowerInvariant() switch
            {
                "resolve" => await _sender.Send(new ResolveReportCaseCommand(reportId, request.ActorUserId, request.Reason, true), ct),
                "escalate" => await _sender.Send(new EscalateReportCaseCommand(reportId, request.ActorUserId, request.Reason), ct),
                _ => Result<ReportDto>.Failure("unprocessable: unsupported bulk action")
            };

            if (result.IsFailure || result.Value is null)
                return Result<IReadOnlyList<ReportDto>>.Failure(result.Error ?? "unprocessable: bulk action failed");
            items.Add(result.Value);
        }

        return Result<IReadOnlyList<ReportDto>>.Success(items);
    }
}
