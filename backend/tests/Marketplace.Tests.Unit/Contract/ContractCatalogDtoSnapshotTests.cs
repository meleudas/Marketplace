using System.Text.Json;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Products.DTOs;

namespace Marketplace.Tests;

public sealed class ContractCatalogDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "CatalogCategories")]
    public void Contract_CategoryDto_Snapshot_Matches()
    {
        var dto = new CategoryDto(
            1,
            "Category",
            "category",
            "https://cdn.test/category.png",
            null,
            "Description",
            "{\"a\":1}",
            5,
            true,
            0,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            false,
            null);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Id\":1,\"Name\":\"Category\",\"Slug\":\"category\",\"ImageUrl\":\"https://cdn.test/category.png\",\"ParentId\":null,\"Description\":\"Description\",\"MetaRaw\":\"{\\u0022a\\u0022:1}\",\"SortOrder\":5,\"IsActive\":true,\"ProductCount\":0,\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\",\"IsDeleted\":false,\"DeletedAt\":null}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "CatalogCategories")]
    public void Contract_ProductSearchResultDto_Snapshot_Matches()
    {
        var item = new ProductListItemDto(
            10,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Keyboard",
            "keyboard",
            "RGB keyboard",
            199,
            null,
            1,
            "active",
            false,
            10,
            1,
            9,
            "in_stock",
            DateTime.UnixEpoch,
            DateTime.UnixEpoch);
        var dto = new ProductSearchResultDto([item], 1, 1, 20, "cursor");

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Items\":[{\"Id\":10,\"CompanyId\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Keyboard\",\"Slug\":\"keyboard\",\"Description\":\"RGB keyboard\",\"Price\":199,\"OldPrice\":null,\"CategoryId\":1,\"Status\":\"active\",\"HasVariants\":false,\"Stock\":10,\"MinStock\":1,\"AvailableQty\":9,\"AvailabilityStatus\":\"in_stock\",\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\"}],\"Total\":1,\"Page\":1,\"PageSize\":20,\"NextSearchAfter\":\"cursor\"}";
        Assert.Equal(expected, json);
    }
}
