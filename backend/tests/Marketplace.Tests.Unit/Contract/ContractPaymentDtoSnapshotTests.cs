using System.Text.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Payments.Ports;

namespace Marketplace.Tests;

public sealed class ContractPaymentDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Payments")]
    public void Contract_LiqPayWebhookRequest_Snapshot_Matches()
    {
        var dto = new LiqPayWebhookRequest("base64-data", "signature");

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Data\":\"base64-data\",\"Signature\":\"signature\"}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Payments")]
    public void Contract_RequestRefundBody_And_LiqPayStatus_Snapshots_Match()
    {
        var refundBody = new RequestRefundBody(125.50m, "manual refund");
        var status = new LiqPayPaymentStatusResult(true, "txn-777", "success", "{\"status\":\"success\"}", null);

        var refundJson = JsonSerializer.Serialize(refundBody);
        var statusJson = JsonSerializer.Serialize(status);

        const string expectedRefund = "{\"Amount\":125.50,\"Reason\":\"manual refund\"}";
        const string expectedStatus = "{\"IsSuccess\":true,\"TransactionId\":\"txn-777\",\"Status\":\"success\",\"RawResponse\":\"{\\u0022status\\u0022:\\u0022success\\u0022}\",\"Error\":null}";
        Assert.Equal(expectedRefund, refundJson);
        Assert.Equal(expectedStatus, statusJson);
    }
}
