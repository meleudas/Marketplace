using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetPersonalizedRecommendations;

public sealed record GetPersonalizedRecommendationsQuery(Guid UserId, int Limit)
    : IRequest<Result<PersonalizedRecommendationsResultDto>>;

public sealed class GetPersonalizedRecommendationsQueryHandler
    : IRequestHandler<GetPersonalizedRecommendationsQuery, Result<PersonalizedRecommendationsResultDto>>
{
    private readonly IPersonalizedRecommendationService _service;

    public GetPersonalizedRecommendationsQueryHandler(IPersonalizedRecommendationService service)
    {
        _service = service;
    }

    public Task<Result<PersonalizedRecommendationsResultDto>> Handle(
        GetPersonalizedRecommendationsQuery request,
        CancellationToken ct)
        => _service.GetForUserAsync(request.UserId, request.Limit, ct);
}
