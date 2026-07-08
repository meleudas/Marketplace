using Marketplace.Domain.Categories.Entities;

namespace Marketplace.Application.Products.Catalog;

public static class CatalogCategoryTree
{
    public static IReadOnlyList<long> ExpandCategoryIds(
        IReadOnlyList<Category> categories,
        IReadOnlyList<long> categoryIds)
    {
        if (categoryIds is not { Count: > 0 })
            return [];

        var childrenByParentId = new Dictionary<long, List<long>>();
        foreach (var category in categories)
        {
            if (category.IsDeleted || category.ParentId is null)
                continue;

            var parentId = category.ParentId!.Value;
            if (!childrenByParentId.TryGetValue(parentId, out var children))
            {
                children = [];
                childrenByParentId[parentId] = children;
            }

            children.Add(category.Id.Value);
        }

        var expanded = new HashSet<long>();
        var stack = new Stack<long>(categoryIds);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!expanded.Add(current))
                continue;

            if (!childrenByParentId.TryGetValue(current, out var children))
                continue;

            foreach (var childId in children)
                stack.Push(childId);
        }

        return expanded.ToList();
    }
}
