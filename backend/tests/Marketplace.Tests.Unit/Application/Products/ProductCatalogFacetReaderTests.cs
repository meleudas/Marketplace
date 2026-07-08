using Marketplace.Application.Products.Catalog;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogFacets")]
public class ProductCatalogFacetReaderTests
{
    [Fact]
    public void Read_Parses_Attributes_And_Genre_Tags()
    {
        var attributes = new JsonBlob("""{"author":"Tolkien","format":"Паперова","genres":["Fantasy","Adventure"]}""");
        var tags = new[] { "genre:sci-fi", "bestseller" };

        var facets = ProductCatalogFacetReader.Read(attributes, tags);

        Assert.Equal("tolkien", facets.Author);
        Assert.Contains("tolkien", facets.AuthorValues);
        Assert.Equal("паперова", facets.Format);
        Assert.Contains("fantasy", facets.Genres);
        Assert.Contains("adventure", facets.Genres);
        Assert.Contains("sci-fi", facets.Genres);
        Assert.Contains("bestseller", facets.Tags);
    }

    [Fact]
    public void Read_Resolves_Author_From_Brands_When_Attributes_Missing()
    {
        var facets = ProductCatalogFacetReader.Read(JsonBlob.Empty, [], ["Марія Матіос"]);

        Assert.Equal("марія матіос", facets.Author);
        Assert.Contains("марія матіос", facets.AuthorValues);
    }

    [Fact]
    public void Read_Resolves_Author_From_Author_Tag()
    {
        var facets = ProductCatalogFacetReader.Read(JsonBlob.Empty, ["author:Tolkien"], []);

        Assert.Equal("tolkien", facets.Author);
        Assert.Contains("tolkien", facets.AuthorValues);
    }

    [Fact]
    public void Read_Canonicalizes_Legacy_Format_Values()
    {
        var attributes = new JsonBlob("""{"format":"паперовий"}""");
        var facets = ProductCatalogFacetReader.Read(attributes, ["format:електронний"], []);

        Assert.Equal("паперова", facets.Format);
    }

    [Fact]
    public void Matches_Requires_All_Requested_Filters()
    {
        var facets = new ProductCatalogFacets("tolkien", ["tolkien"], "паперова", ["fantasy"], ["bestseller"]);

        Assert.True(ProductCatalogFacetReader.Matches(facets, ["Tolkien"], "паперова", ["fantasy"], ["bestseller"]));
        Assert.False(ProductCatalogFacetReader.Matches(facets, ["Rowling"], null, null, null));
        Assert.False(ProductCatalogFacetReader.Matches(facets, null, "електронна", null, null));
        Assert.False(ProductCatalogFacetReader.Matches(facets, null, null, ["horror"], null));
        Assert.False(ProductCatalogFacetReader.Matches(facets, null, null, null, ["new"]));
    }

    [Fact]
    public void Matches_Does_Not_Accept_Frontend_Hardcover_Alias()
    {
        var facets = new ProductCatalogFacets("tolkien", ["tolkien"], "паперова", ["fantasy"], []);

        Assert.False(ProductCatalogFacetReader.Matches(facets, null, "hardcover", null, null));
    }

    [Fact]
    public void Matches_Authors_Uses_Or_Logic()
    {
        var facets = new ProductCatalogFacets("tolkien", ["tolkien"], "паперова", ["fantasy"], []);

        Assert.True(ProductCatalogFacetReader.MatchesAnyAuthor(facets, ["Tolkien", "Rowling"]));
        Assert.False(ProductCatalogFacetReader.MatchesAnyAuthor(facets, ["Rowling", "Martin"]));
    }

    [Fact]
    public void Matches_Authors_Uses_All_AuthorValues()
    {
        var facets = new ProductCatalogFacets("tolkien", ["tolkien", "j.r.r. tolkien"], "паперова", [], []);

        Assert.True(ProductCatalogFacetReader.MatchesAnyAuthor(facets, ["j.r.r. tolkien"]));
    }

    [Fact]
    public void Matches_Genres_Uses_Or_Logic()
    {
        var facets = new ProductCatalogFacets("tolkien", ["tolkien"], "паперова", ["fantasy"], []);

        Assert.True(ProductCatalogFacetReader.MatchesAnyGenre(facets, ["fantasy", "horror"]));
        Assert.False(ProductCatalogFacetReader.MatchesAnyGenre(facets, ["horror", "sci-fi"]));
    }

    [Fact]
    public void Matches_Genres_Falls_Back_To_Tags()
    {
        var facets = new ProductCatalogFacets("tolkien", ["tolkien"], "паперова", [], ["фентезі", "популярне"]);

        Assert.True(ProductCatalogFacetReader.MatchesAnyGenre(facets, ["фентезі"]));
        Assert.False(ProductCatalogFacetReader.MatchesAnyGenre(facets, ["детектив"]));
    }
}
