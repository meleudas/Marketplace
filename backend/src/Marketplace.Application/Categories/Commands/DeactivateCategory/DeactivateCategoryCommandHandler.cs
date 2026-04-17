using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Result>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCachePort _cache;

    public DeactivateCategoryCommandHandler(ICategoryRepository categoryRepository, IAppCachePort cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(DeactivateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
            if (category == null)
                return Result.Failure("Category not found");

            category.Deactivate();
            await _categoryRepository.UpdateAsync(category, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ActiveCategories, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AllCategories, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCategoryByIdPrefix + category.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCategoryByIdPrefix + category.Id.Value, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to deactivate category: {ex.Message}");
        }
    }
}
