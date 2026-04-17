using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Mappings;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Categories.Queries.GetAllCategories;

public sealed class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync<List<CategoryDto>>(CatalogCacheKeys.AllCategories, ct);
            if (cached is not null)
                return Result<IReadOnlyList<CategoryDto>>.Success(cached);

            var categories = await _categoryRepository.GetAllAsync(ct);
            var dtos = categories.Select(CategoryMapper.ToDto).ToList();
            await _cache.SetAsync(CatalogCacheKeys.AllCategories, dtos, _ttl.AdminAllCategories, ct);
            return Result<IReadOnlyList<CategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CategoryDto>>.Failure($"Failed to get categories: {ex.Message}");
        }
    }
}
