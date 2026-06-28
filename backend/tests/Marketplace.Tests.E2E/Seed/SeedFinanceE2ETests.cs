using System.Net;
using System.Net.Http.Json;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Seed;

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Seed")]
[Trait("Layer", "E2E")]
public sealed class SeedFinanceE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public SeedFinanceE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Seller_Earnings_Summary_Has_Positive_Available_And_Fees()
    {
        var seller = await _fixture.LoginSeedUserAsync(SeedTestConstants.SellerEmail);
        var companyId = SeedTestConstants.TechStoreCompanyId;
        var response = await seller.GetAsync($"/companies/{companyId}/earnings/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<SellerEarningsSummaryDto>(E2EJsonOptions.Default);
        Assert.NotNull(summary);
        Assert.True(summary!.AvailableAmount > 0);
        Assert.True(summary.TotalPlatformFees > 0);
    }

    [Fact]
    public async Task Seller_Can_List_Settlements_With_Ready_Batch()
    {
        var seller = await _fixture.LoginSeedUserAsync(SeedTestConstants.SellerEmail);
        var companyId = SeedTestConstants.TechStoreCompanyId;
        var response = await seller.GetAsync($"/companies/{companyId}/settlements");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(SeedTestConstants.SettlementBatchReadyId.ToString(), body);
    }

    [Fact]
    public async Task Admin_Can_List_Ready_Settlements()
    {
        var admin = await _fixture.LoginSeedUserAsync(SeedTestConstants.AdminEmail);
        var response = await admin.GetAsync($"/admin/settlements?status={(short)SettlementBatchStatus.Ready}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(SeedTestConstants.SettlementBatchReadyId.ToString(), body);
    }
}
