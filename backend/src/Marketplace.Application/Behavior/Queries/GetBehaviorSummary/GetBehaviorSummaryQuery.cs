using Marketplace.Application.Behavior.DTOs;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Queries.GetBehaviorSummary;

public sealed record GetBehaviorSummaryQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<Result<BehaviorSummaryDto>>;

public sealed class GetBehaviorSummaryQueryHandler : IRequestHandler<GetBehaviorSummaryQuery, Result<BehaviorSummaryDto>>
{
    private readonly IBehaviorEventRepository _repository;

    public GetBehaviorSummaryQueryHandler(IBehaviorEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BehaviorSummaryDto>> Handle(GetBehaviorSummaryQuery request, CancellationToken ct)
    {
        var from = request.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = request.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var views = await _repository.CountByTypeAsync(BehaviorEventType.ProductView, from, to, ct);
        var searches = await _repository.CountByTypeAsync(BehaviorEventType.SearchQuery, from, to, ct);
        var clicks = await _repository.CountByTypeAsync(BehaviorEventType.CatalogClick, from, to, ct);
        var adds = await _repository.CountByTypeAsync(BehaviorEventType.AddToCart, from, to, ct);
        return Result<BehaviorSummaryDto>.Success(
            new BehaviorSummaryDto(request.FromDate, request.ToDate, views + searches + clicks + adds, views, searches, clicks, adds));
    }
}
