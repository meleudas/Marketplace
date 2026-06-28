using System.Net;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Seed;

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Seed")]
[Trait("Layer", "E2E")]
public sealed class SeedCouponReturnChatE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public SeedCouponReturnChatE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Buyer_Can_Validate_SEED10_Coupon()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var response = await buyer.PostAsJsonAsync(
            "/me/cart/coupons/validate",
            new CouponCodeRequest(SeedTestConstants.CouponCode),
            E2EJsonOptions.Default);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Buyer_Has_Seed_Return_Request()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var response = await buyer.GetAsync("/me/returns");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(SeedTestConstants.ReturnRequestId.ToString(), body);
    }

    [Fact]
    public async Task Buyer_And_Seller_See_Order_Chat()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var buyerList = await buyer.GetAsync("/me/chats");
        Assert.Equal(HttpStatusCode.OK, buyerList.StatusCode);
        var buyerBody = await buyerList.Content.ReadAsStringAsync();
        Assert.Contains(SeedTestConstants.OrderChatId.ToString(), buyerBody);

        var seller = await _fixture.LoginSeedUserAsync(SeedTestConstants.SellerEmail);
        var sellerList = await seller.GetAsync("/me/chats");
        Assert.Equal(HttpStatusCode.OK, sellerList.StatusCode);
        var sellerBody = await sellerList.Content.ReadAsStringAsync();
        Assert.Contains(SeedTestConstants.OrderChatId.ToString(), sellerBody);
    }

    [Fact]
    public async Task Buyer_Can_Send_Message_In_Order_Chat()
    {
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var chatId = SeedTestConstants.OrderChatId;
        var response = await buyer.PostAsJsonAsync(
            $"/me/chats/{chatId}/messages",
            new SendChatMessageRequest("E2E follow-up on ORD-SEED-0002", null),
            E2EJsonOptions.Default);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
