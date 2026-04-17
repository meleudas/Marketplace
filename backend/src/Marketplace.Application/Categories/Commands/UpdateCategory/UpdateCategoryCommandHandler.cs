using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Mappings;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCachePort _cache;

    public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IAppCachePort cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var id = CategoryId.From(request.CategoryId);
            var category = await _categoryRepository.GetByIdAsync(id, ct);
            if (category == null)
                return Result<CategoryDto>.Failure("Category not found");

            category.UpdateDetails(
                request.Name,
                request.Slug,
                request.ImageUrl,
                request.ParentCategoryId.HasValue ? CategoryId.From(request.ParentCategoryId.Value) : null,
                request.Description,
                new JsonBlob(request.MetaRaw),
                request.SortOrder);

            await _categoryRepository.UpdateAsync(category, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ActiveCategories, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AllCategories, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCategoryByIdPrefix + category.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCategoryByIdPrefix + category.Id.Value, ct);
            return Result<CategoryDto>.Success(CategoryMapper.ToDto(category));
        }
        catch (Exception ex)
        {
            return Result<CategoryDto>.Failure($"Failed to update category: {ex.Message}");
        }
    }
}
