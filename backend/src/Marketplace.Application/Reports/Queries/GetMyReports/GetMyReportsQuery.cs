using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reports.Queries.GetMyReports;

public sealed record GetMyReportsQuery(string ActorUserId) : IRequest<Result<IReadOnlyList<ReportDto>>>;

public sealed class GetMyReportsQueryHandler : IRequestHandler<GetMyReportsQuery, Result<IReadOnlyList<ReportDto>>>
{
    private readonly IReportRepository _reportRepository;

    public GetMyReportsQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<IReadOnlyList<ReportDto>>> Handle(GetMyReportsQuery request, CancellationToken ct)
    {
        var items = await _reportRepository.ListByReporterAsync(request.ActorUserId, ct);
        return Result<IReadOnlyList<ReportDto>>.Success(items.Select(x => x.ToDto()).ToList());
    }
}
