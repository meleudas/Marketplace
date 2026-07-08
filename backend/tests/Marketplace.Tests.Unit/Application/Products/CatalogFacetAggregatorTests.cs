using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.Ports;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogFacets")]
public class CatalogFacetAggregatorTests
{
    private readonly CatalogFacetAggregator _aggregator = new();

    [Fact]
    public void Aggregate_Collects_Authors_Genres_Formats_And_Tags_With_Counts()
    {
        var sources = new List<ProductFacetSourceRow>
        {
            new(1, """{"author":"Tolkien","format":"Паперова","genre":"fantasy"}""", ["bestseller"], []),
            new(2, """{"author":"Tolkien","format":"паперовий","genres":["Fantasy"]}""", ["genre:sci-fi"], []),
            new(3, null, ["author:Rowling", "format:електронна"], ["Марія Матіос"])
        };

        var result = _aggregator.Aggregate(sources);

        Assert.Contains(result.Authors, x => x.Value == "tolkien" && x.Label == "Tolkien" && x.Count == 2);
        Assert.Contains(result.Authors, x => x.Value == "rowling" && x.Count == 1);
        Assert.Contains(result.Authors, x => x.Value == "марія матіос" && x.Label == "Марія Матіос" && x.Count == 1);
        Assert.Contains(result.Genres, x => x.Value == "fantasy");
        Assert.Contains(result.Genres, x => x.Value == "sci-fi");
        Assert.Contains(result.Formats, x => x.Value == "паперова" && x.Label == "Паперова" && x.Count == 2);
        Assert.Contains(result.Formats, x => x.Value == "електронна" && x.Label == "Електронна" && x.Count == 1);
        Assert.Contains(result.Tags, x => x.Value == "bestseller" && x.Count == 1);
    }

    [Fact]
    public void Aggregate_Sorts_Options_By_Label()
    {
        var sources = new List<ProductFacetSourceRow>
        {
            new(1, """{"author":"Zorro"}""", [], []),
            new(2, """{"author":"Alpha"}""", [], []),
            new(3, """{"author":"Beta"}""", [], [])
        };

        var result = _aggregator.Aggregate(sources);

        Assert.Equal(["alpha", "beta", "zorro"], result.Authors.Select(x => x.Value).ToArray());
    }
}
