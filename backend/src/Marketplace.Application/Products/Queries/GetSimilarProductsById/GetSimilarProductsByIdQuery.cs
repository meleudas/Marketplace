using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetSimilarProductsById;

public sealed record GetSimilarProductsByIdQuery(long ProductId, int Limit) : IRequest<Result<SimilarProductsResultDto>>;

public sealed class GetSimilarProductsByIdQueryHandler : IRequestHandler<GetSimilarProductsByIdQuery, Result<SimilarProductsResultDto>>
{
    private readonly SimilarProductsOrchestrator _orchestrator;

    public GetSimilarProductsByIdQueryHandler(SimilarProductsOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<Result<SimilarProductsResultDto>> Handle(GetSimilarProductsByIdQuery request, CancellationToken ct)
        => _orchestrator.GetByIdAsync(request.ProductId, request.Limit, ct);
}
