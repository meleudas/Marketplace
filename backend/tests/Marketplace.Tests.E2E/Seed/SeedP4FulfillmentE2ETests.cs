using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Seed;

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Seed")]
[Trait("Layer", "E2E")]
public sealed class SeedP4FulfillmentE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public SeedP4FulfillmentE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Order4_Has_PendingByWarehouse_From_Two_Warehouses()
    {
        var seller = await _fixture.LoginSeedUserAsync(SeedTestConstants.SellerEmail);
        var companyId = SeedTestConstants.TechStoreCompanyId;
        var response = await seller.GetAsync($"/companies/{companyId}/orders/{SeedTestConstants.OrderPaidSplitId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var fulfillment = doc.RootElement.GetProperty("fulfillment");
        var pendingByWarehouse = fulfillment.GetProperty("pendingByWarehouse");
        Assert.Equal(2, pendingByWarehouse.GetArrayLength());
    }

    [Fact]
    public async Task Order2_Has_Two_Company_Shipments_Per_Warehouse()
    {
        var seller = await _fixture.LoginSeedUserAsync(SeedTestConstants.SellerEmail);
        var companyId = SeedTestConstants.TechStoreCompanyId;
        var response = await seller.GetAsync($"/companies/{companyId}/orders/{SeedTestConstants.OrderShippedId}/shipments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var shipments = await response.Content.ReadFromJsonAsync<IReadOnlyList<ShipmentSummaryDto>>(E2EJsonOptions.Default);
        Assert.NotNull(shipments);
        Assert.Equal(2, shipments.Count);
    }

    [Fact]
    public async Task Buyer_Can_List_Order2_Shipments()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var response = await buyer.GetAsync($"/me/orders/{SeedTestConstants.OrderShippedId}/shipments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var shipments = await response.Content.ReadFromJsonAsync<IReadOnlyList<ShipmentSummaryDto>>(E2EJsonOptions.Default);
        Assert.NotNull(shipments);
        Assert.Equal(2, shipments!.Count);
    }
}
