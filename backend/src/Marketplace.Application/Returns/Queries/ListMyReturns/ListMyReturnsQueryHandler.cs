using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Queries.ListMyReturns;

public sealed record ListMyReturnsQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<ReturnRequestSummaryDto>>>;

public sealed class ListMyReturnsQueryHandler : IRequestHandler<ListMyReturnsQuery, Result<IReadOnlyList<ReturnRequestSummaryDto>>>
{
    private readonly IReturnRequestRepository _returnRepository;

    public ListMyReturnsQueryHandler(IReturnRequestRepository returnRepository) => _returnRepository = returnRepository;

    public async Task<Result<IReadOnlyList<ReturnRequestSummaryDto>>> Handle(ListMyReturnsQuery request, CancellationToken ct)
    {
        var items = await _returnRepository.ListByCustomerAsync(request.CustomerId, ct);
        return Result<IReadOnlyList<ReturnRequestSummaryDto>>.Success(
            items.Select(ReturnMapper.ToSummary).ToList());
    }
}
