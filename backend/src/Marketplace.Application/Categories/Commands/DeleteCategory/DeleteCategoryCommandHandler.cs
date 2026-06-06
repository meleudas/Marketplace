using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;

    public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IProductRepository productRepository, IAppCachePort cache)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
            if (category == null)
                return Result.Failure("Category not found");

            var allCategories = await _categoryRepository.GetAllAsync(ct);
            if (allCategories.Any(x => !x.IsDeleted && x.ParentId?.Value == category.Id.Value))
                return Result.Failure("Cannot delete category with active child categories");

            var activeProducts = await _productRepository.ListActiveAsync(ct);
            if (activeProducts.Any(x => x.CategoryId.Value == category.Id.Value))
                return Result.Failure("Cannot delete category that still has active products");

            category.SoftDelete();
            await _categoryRepository.UpdateAsync(category, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ActiveCategories, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AllCategories, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.CatalogCategoryByIdPrefix + category.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.AdminCategoryByIdPrefix + category.Id.Value, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete category: {ex.Message}");
        }
    }
}
