using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Mappings;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Queries.GetActiveCategories;

public sealed class GetActiveCategoriesQueryHandler : IRequestHandler<GetActiveCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCachePort _cache;

    public GetActiveCategoriesQueryHandler(ICategoryRepository categoryRepository, IAppCachePort cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetActiveCategoriesQuery request, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync<List<CategoryDto>>(CatalogCacheKeys.ActiveCategories, ct);
            if (cached is not null)
                return Result<IReadOnlyList<CategoryDto>>.Success(cached);

            var categories = await _categoryRepository.GetActiveAsync(ct);
            var dtos = categories.Select(CategoryMapper.ToDto).ToList();
            await _cache.SetAsync(CatalogCacheKeys.ActiveCategories, dtos, TimeSpan.FromMinutes(10), ct);
            return Result<IReadOnlyList<CategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CategoryDto>>.Failure($"Failed to get active categories: {ex.Message}");
        }
    }
}
