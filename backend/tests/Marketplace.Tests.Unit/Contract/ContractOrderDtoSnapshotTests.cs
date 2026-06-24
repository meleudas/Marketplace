using System.Text.Json;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Application.Returns.DTOs;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Tests;

public sealed class ContractOrderDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Orders")]
    public void Contract_OrderListItemDto_Snapshot_Matches()
    {
        var dto = new OrderListItemDto(
            17,
            "ORD-17",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            OrderStatus.Processing,
            459.90m,
            "Card",
            DateTime.UnixEpoch,
            DateTime.UnixEpoch);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"OrderId\":17,\"OrderNumber\":\"ORD-17\",\"CustomerId\":\"11111111-1111-1111-1111-111111111111\",\"CompanyId\":\"22222222-2222-2222-2222-222222222222\",\"Status\":2,\"TotalPrice\":459.90,\"PaymentMethod\":\"Card\",\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\"}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Orders")]
    public void Contract_OrderDetailsDto_Snapshot_Matches()
    {
        var dto = new OrderDetailsDto(
            18,
            "ORD-18",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            OrderStatus.Shipped,
            300m,
            250m,
            30m,
            10m,
            30m,
            "Card",
            "deliver soon",
            "TRK-18",
            DateTime.UnixEpoch,
            null,
            null,
            null,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            [new OrderItemDto(99, 5, "Keyboard", null, 1, 250m, 10m, 240m)],
            [new OrderAddressDto("Shipping", "A", "B", "+380", "Street", "Kyiv", "Kyiv", "01001", "UA")],
            new PaymentSnapshotDto(77, "LiqPay", 300m, "UAH", "txn-1", "Completed", DateTime.UnixEpoch),
            [new RefundSnapshotDto(0, 0m, "none", "None", null, null, DateTime.UnixEpoch)],
            [],
            [new OrderStatusHistoryDto("Paid", "Shipped", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "company", "manual", null, null, DateTime.UnixEpoch)],
            new FulfillmentReadinessDto(1, 1, 0, true, false, [], [], []));

        var json = JsonSerializer.Serialize(dto);
        Assert.Contains("\"OrderItemId\":99", json);
        Assert.Contains("\"Fulfillment\"", json);
        Assert.Contains("\"Returns\"", json);
    }
}
