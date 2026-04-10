using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.ActivateCategory;

public sealed class ActivateCategoryCommandHandler : IRequestHandler<ActivateCategoryCommand, Result>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCachePort _cache;

    public ActivateCategoryCommandHandler(ICategoryRepository categoryRepository, IAppCachePort cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(ActivateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
            if (category == null)
                return Result.Failure("Category not found");

            category.Activate();
            await _categoryRepository.UpdateAsync(category, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ActiveCategories, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to activate category: {ex.Message}");
        }
    }
}
