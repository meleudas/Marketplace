using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetPersonalizedRecommendations;

public sealed record GetPersonalizedRecommendationsQuery(Guid UserId, int Limit)
    : IRequest<Result<PersonalizedRecommendationsResultDto>>;

public sealed class GetPersonalizedRecommendationsQueryHandler
    : IRequestHandler<GetPersonalizedRecommendationsQuery, Result<PersonalizedRecommendationsResultDto>>
{
    private readonly IPersonalizedRecommendationService _service;
    private readonly IProductImageRepository _productImageRepository;

    public GetPersonalizedRecommendationsQueryHandler(
        IPersonalizedRecommendationService service,
        IProductImageRepository productImageRepository)
    {
        _service = service;
        _productImageRepository = productImageRepository;
    }

    public async Task<Result<PersonalizedRecommendationsResultDto>> Handle(
        GetPersonalizedRecommendationsQuery request,
        CancellationToken ct)
    {
        var result = await _service.GetForUserAsync(request.UserId, request.Limit, ct);
        if (!result.IsSuccess || result.Value is null)
            return result;

        var items = await ProductListImageEnricher.WithImageUrlsAsync(
            result.Value.Items,
            _productImageRepository,
            ct);

        return Result<PersonalizedRecommendationsResultDto>.Success(result.Value with { Items = items });
    }
}
