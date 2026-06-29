using System.Net;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Returns;

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "Returns")]
[Trait("Layer", "E2E")]
public sealed class ReturnsRefundWorkflowE2ETests
{
    private readonly MarketplaceSeededE2EFixture _fixture;

    public ReturnsRefundWorkflowE2ETests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Admin_Refund_Completes_Seeded_Return_Workflow()
    {
        var companyId = SeedTestConstants.HomeComfortCompanyId;
        var returnId = SeedTestConstants.ReturnRequestId;

        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        buyer.DefaultRequestHeaders.Add("Idempotency-Key", $"e2e-return-approve-{returnId}");

        var approve = await buyer.PostAsync($"/companies/{companyId}/returns/{returnId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        buyer.DefaultRequestHeaders.Remove("Idempotency-Key");
        buyer.DefaultRequestHeaders.Add("Idempotency-Key", $"e2e-return-received-{returnId}");
        var received = await buyer.PostAsync($"/companies/{companyId}/returns/{returnId}/received", null);
        Assert.Equal(HttpStatusCode.OK, received.StatusCode);

        var admin = await _fixture.LoginSeedUserAsync(SeedTestConstants.AdminEmail);
        admin.DefaultRequestHeaders.Add("Idempotency-Key", $"e2e-return-refund-{returnId}");
        var refund = await admin.PostAsJsonAsync(
            $"/admin/returns/{returnId}/refund",
            new ProcessReturnRefundBody(899m),
            E2EJsonOptions.Default);
        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);

        var body = await refund.Content.ReadAsStringAsync();
        Assert.Contains("Refunded", body, StringComparison.OrdinalIgnoreCase);
    }
}
