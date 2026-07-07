using Marketplace.Application.Products.Queries.SearchCatalogProducts;

namespace Marketplace.Tests;

[Trait("Suite", "CatalogCategories")]
public sealed class ApplicationSearchCatalogProductsValidatorTests
{
    private readonly SearchCatalogProductsQueryValidator _validator = new();

    [Fact]
    public void Fails_When_Page_Is_Less_Than_1()
    {
        var result = _validator.Validate(new SearchCatalogProductsQuery(null, null, null, null, null, null, null, null, null, null, null, null, 0, 20, null));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchCatalogProductsQuery.Page));
    }

    [Fact]
    public void Fails_When_PageSize_Out_Of_Range()
    {
        var result = _validator.Validate(new SearchCatalogProductsQuery(null, null, null, null, null, null, null, null, null, null, null, null, 1, 150, null));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchCatalogProductsQuery.PageSize));
    }

    [Fact]
    public void Fails_When_MinPrice_Greater_Than_MaxPrice()
    {
        var result = _validator.Validate(new SearchCatalogProductsQuery(null, null, null, null, 100, 10, null, null, null, null, null, null, 1, 20, null));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("MinPrice", StringComparison.OrdinalIgnoreCase));
    }
}
