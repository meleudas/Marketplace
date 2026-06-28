using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reports.Queries.GetModerationQueue;

public sealed record GetModerationQueueQuery(int Limit = 100) : IRequest<Result<IReadOnlyList<ReportDto>>>;

public sealed class GetModerationQueueQueryHandler : IRequestHandler<GetModerationQueueQuery, Result<IReadOnlyList<ReportDto>>>
{
    private readonly IReportRepository _reportRepository;

    public GetModerationQueueQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<IReadOnlyList<ReportDto>>> Handle(GetModerationQueueQuery request, CancellationToken ct)
    {
        var items = await _reportRepository.ListModerationQueueAsync(request.Limit, ct);
        return Result<IReadOnlyList<ReportDto>>.Success(items.Select(x => x.ToDto()).ToList());
    }
}
