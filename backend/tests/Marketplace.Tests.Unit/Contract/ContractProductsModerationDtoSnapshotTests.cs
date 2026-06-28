using System.Text.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Products.DTOs;

namespace Marketplace.Tests;

public sealed class ContractProductsModerationDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "ProductsModeration")]
    public void Contract_PendingProductModerationDto_Snapshot_Matches()
    {
        var dto = new PendingProductModerationDto(
            10,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Keyboard",
            "keyboard",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DateTime.UnixEpoch);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"ProductId\":10,\"CompanyId\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Keyboard\",\"Slug\":\"keyboard\",\"SubmittedByUserId\":\"22222222-2222-2222-2222-222222222222\",\"CreatedAt\":\"1970-01-01T00:00:00Z\"}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "ProductsModeration")]
    public void Contract_RejectProductBody_Snapshot_Matches()
    {
        var dto = new RejectProductBody("photos are blurred");

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Reason\":\"photos are blurred\"}";
        Assert.Equal(expected, json);
    }
}
