using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetSimilarProductsBySlug;

public sealed record GetSimilarProductsBySlugQuery(string Slug, int Limit) : IRequest<Result<SimilarProductsResultDto>>;

public sealed class GetSimilarProductsBySlugQueryHandler : IRequestHandler<GetSimilarProductsBySlugQuery, Result<SimilarProductsResultDto>>
{
    private readonly SimilarProductsOrchestrator _orchestrator;

    public GetSimilarProductsBySlugQueryHandler(SimilarProductsOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<Result<SimilarProductsResultDto>> Handle(GetSimilarProductsBySlugQuery request, CancellationToken ct)
        => _orchestrator.GetBySlugAsync(request.Slug, request.Limit, ct);
}
