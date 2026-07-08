using Marketplace.Domain.Categories.Repositories;

namespace Marketplace.Application.Products.Catalog;

public static class CatalogCategoryFilterExpander
{
    public static async Task<IReadOnlyList<long>?> ExpandAsync(
        ICategoryRepository categoryRepository,
        IReadOnlyList<long>? categoryIds,
        CancellationToken ct = default)
    {
        if (categoryIds is not { Count: > 0 })
            return categoryIds;

        var categories = await categoryRepository.GetActiveAsync(ct);
        return CatalogCategoryTree.ExpandCategoryIds(categories, categoryIds);
    }
}
