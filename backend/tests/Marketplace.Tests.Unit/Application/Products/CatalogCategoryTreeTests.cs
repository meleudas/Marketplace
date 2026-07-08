using Marketplace.Application.Products.Catalog;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests.Unit.Application.Products;

public sealed class CatalogCategoryTreeTests
{
    [Fact]
    public void ExpandCategoryIds_Root_Includes_All_Descendants()
    {
        var categories = new[]
        {
            Category.Create(CategoryId.From(1), "Fiction", "fiction", null, null, null, JsonBlob.Empty, 1),
            Category.Create(CategoryId.From(11), "Novels", "fiction-novels", null, CategoryId.From(1), null, JsonBlob.Empty, 1),
            Category.Create(CategoryId.From(13), "Fantasy", "fiction-fantasy", null, CategoryId.From(1), null, JsonBlob.Empty, 2),
        };

        var expanded = CatalogCategoryTree.ExpandCategoryIds(categories, [1]);

        Assert.Contains(1L, expanded);
        Assert.Contains(11L, expanded);
        Assert.Contains(13L, expanded);
    }

    [Fact]
    public void ExpandCategoryIds_Leaf_Returns_Only_That_Id()
    {
        var categories = new[]
        {
            Category.Create(CategoryId.From(1), "Fiction", "fiction", null, null, null, JsonBlob.Empty, 1),
            Category.Create(CategoryId.From(13), "Fantasy", "fiction-fantasy", null, CategoryId.From(1), null, JsonBlob.Empty, 1),
        };

        var expanded = CatalogCategoryTree.ExpandCategoryIds(categories, [13]);

        Assert.Equal([13L], expanded);
    }

    [Fact]
    public void ExpandCategoryIds_Multiple_Roots_Unions_Descendants()
    {
        var categories = new[]
        {
            Category.Create(CategoryId.From(1), "Fiction", "fiction", null, null, null, JsonBlob.Empty, 1),
            Category.Create(CategoryId.From(11), "Novels", "fiction-novels", null, CategoryId.From(1), null, JsonBlob.Empty, 1),
            Category.Create(CategoryId.From(2), "Documentary", "documentary", null, null, null, JsonBlob.Empty, 2),
            Category.Create(CategoryId.From(21), "Biographies", "documentary-biographies", null, CategoryId.From(2), null, JsonBlob.Empty, 1),
        };

        var expanded = CatalogCategoryTree.ExpandCategoryIds(categories, [1, 2]);

        Assert.Contains(1L, expanded);
        Assert.Contains(11L, expanded);
        Assert.Contains(2L, expanded);
        Assert.Contains(21L, expanded);
    }
}
