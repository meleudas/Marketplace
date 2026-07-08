using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCatalogProductFacets;

public sealed class GetCatalogProductFacetsQueryHandler
    : IRequestHandler<GetCatalogProductFacetsQuery, Result<CatalogProductFacetsDto>>
{
    private readonly ICatalogFacetReadService _facetReadService;
    private readonly ICategoryRepository _categoryRepository;

    public GetCatalogProductFacetsQueryHandler(
        ICatalogFacetReadService facetReadService,
        ICategoryRepository categoryRepository)
    {
        _facetReadService = facetReadService;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<CatalogProductFacetsDto>> Handle(GetCatalogProductFacetsQuery request, CancellationToken ct)
    {
        try
        {
            var categoryIds = await CatalogCategoryFilterExpander.ExpandAsync(_categoryRepository, request.CategoryIds, ct);
            var facets = await _facetReadService.GetFacetsAsync(categoryIds, request.CompanyId, ct);
            return Result<CatalogProductFacetsDto>.Success(facets);
        }
        catch (Exception ex)
        {
            return Result<CatalogProductFacetsDto>.Failure($"Failed to get catalog product facets: {ex.Message}");
        }
    }
}
