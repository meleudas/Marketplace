using Marketplace.Application.Products.Catalog;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogFacets")]
public class ProductCatalogFacetReaderTests
{
    [Fact]
    public void Read_Parses_Attributes_And_Genre_Tags()
    {
        var attributes = new JsonBlob("""{"author":"Tolkien","format":"Hardcover","genres":["Fantasy","Adventure"]}""");
        var tags = new[] { "genre:sci-fi", "bestseller" };

        var facets = ProductCatalogFacetReader.Read(attributes, tags);

        Assert.Equal("tolkien", facets.Author);
        Assert.Equal("hardcover", facets.Format);
        Assert.Contains("fantasy", facets.Genres);
        Assert.Contains("adventure", facets.Genres);
        Assert.Contains("sci-fi", facets.Genres);
        Assert.Contains("bestseller", facets.Tags);
    }

    [Fact]
    public void Matches_Requires_All_Requested_Filters()
    {
        var facets = new ProductCatalogFacets("tolkien", "hardcover", ["fantasy"], ["bestseller"]);

        Assert.True(ProductCatalogFacetReader.Matches(facets, "Tolkien", "hardcover", "fantasy", ["bestseller"]));
        Assert.False(ProductCatalogFacetReader.Matches(facets, "Rowling", null, null, null));
        Assert.False(ProductCatalogFacetReader.Matches(facets, null, "paperback", null, null));
        Assert.False(ProductCatalogFacetReader.Matches(facets, null, null, "horror", null));
        Assert.False(ProductCatalogFacetReader.Matches(facets, null, null, null, ["new"]));
    }
}
