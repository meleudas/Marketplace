using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Mappings;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Categories.Queries.GetCatalogCategoryById;

public sealed class GetCatalogCategoryByIdQueryHandler : IRequestHandler<GetCatalogCategoryByIdQuery, Result<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetCatalogCategoryByIdQueryHandler(ICategoryRepository categoryRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<CategoryDto>> Handle(GetCatalogCategoryByIdQuery request, CancellationToken ct)
    {
        try
        {
            var cacheKey = CatalogCacheKeys.CatalogCategoryByIdPrefix + request.CategoryId;
            var cached = await _cache.GetAsync<CategoryDto>(cacheKey, ct);
            if (cached is not null)
                return Result<CategoryDto>.Success(cached);

            var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
            if (category is null || category.IsDeleted || !category.IsActive)
                return Result<CategoryDto>.Failure("Category not found");

            var dto = CategoryMapper.ToDto(category);
            await _cache.SetAsync(cacheKey, dto, _ttl.CatalogCategory, ct);
            return Result<CategoryDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<CategoryDto>.Failure($"Failed to get category: {ex.Message}");
        }
    }
}
