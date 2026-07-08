using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCatalogAuthors;

public sealed class GetCatalogAuthorsQueryHandler
    : IRequestHandler<GetCatalogAuthorsQuery, Result<IReadOnlyList<CatalogFacetOptionDto>>>
{
    private readonly ICatalogFacetReadService _facetReadService;
    private readonly ICategoryRepository _categoryRepository;

    public GetCatalogAuthorsQueryHandler(
        ICatalogFacetReadService facetReadService,
        ICategoryRepository categoryRepository)
    {
        _facetReadService = facetReadService;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IReadOnlyList<CatalogFacetOptionDto>>> Handle(GetCatalogAuthorsQuery request, CancellationToken ct)
    {
        try
        {
            var categoryIds = await CatalogCategoryFilterExpander.ExpandAsync(_categoryRepository, request.CategoryIds, ct);
            var facets = await _facetReadService.GetFacetsAsync(categoryIds, request.CompanyId, ct);
            return Result<IReadOnlyList<CatalogFacetOptionDto>>.Success(facets.Authors);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CatalogFacetOptionDto>>.Failure($"Failed to get catalog authors: {ex.Message}");
        }
    }
}
