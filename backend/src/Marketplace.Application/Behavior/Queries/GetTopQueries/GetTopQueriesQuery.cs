using Marketplace.Application.Behavior.DTOs;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Queries.GetTopQueries;

public sealed record GetTopQueriesQuery(DateOnly FromDate, DateOnly ToDate, int Limit = 10) : IRequest<Result<IReadOnlyList<TopQueryDto>>>;

public sealed class GetTopQueriesQueryHandler : IRequestHandler<GetTopQueriesQuery, Result<IReadOnlyList<TopQueryDto>>>
{
    private readonly ISearchQueryAggregateRepository _repository;

    public GetTopQueriesQueryHandler(ISearchQueryAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TopQueryDto>>> Handle(GetTopQueriesQuery request, CancellationToken ct)
    {
        var items = await _repository.GetTopQueriesAsync(request.FromDate, request.ToDate, request.Limit, ct);
        return Result<IReadOnlyList<TopQueryDto>>.Success(items.Select(x => new TopQueryDto(x.Query, x.Count)).ToList());
    }
}
