using System.Text.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Notifications.Enums;

namespace Marketplace.Tests;

public sealed class ContractNotificationDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Notifications")]
    public void Contract_PagedInAppNotificationsDto_Snapshot_Matches()
    {
        var dto = new PagedInAppNotificationsDto(
            [
                new InAppNotificationListItemDto(
                    11,
                    AppNotificationTemplateKeys.UserOrderStatus,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    NotificationKind.Order,
                    "Status updated",
                    "Order has been shipped",
                    "/orders/11",
                    false,
                    null,
                    DateTime.UnixEpoch,
                    "{\"templateKey\":\"UserOrderStatus\",\"orderId\":11}")
            ],
            1,
            1,
            20);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Items\":[{\"Id\":11,\"TemplateKey\":\"UserOrderStatus\",\"CorrelationId\":\"11111111-1111-1111-1111-111111111111\",\"Kind\":0,\"Title\":\"Status updated\",\"Message\":\"Order has been shipped\",\"ActionUrl\":\"/orders/11\",\"IsRead\":false,\"ReadAt\":null,\"CreatedAtUtc\":\"1970-01-01T00:00:00Z\",\"DataJson\":\"{\\u0022templateKey\\u0022:\\u0022UserOrderStatus\\u0022,\\u0022orderId\\u0022:11}\"}],\"Total\":1,\"Page\":1,\"PageSize\":20}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Notifications")]
    public void Contract_PushSubscriptionRequest_And_VapidResponse_Snapshots_Match()
    {
        var request = new PushNotificationsController.RegisterPushSubscriptionRequest
        {
            Endpoint = "https://push.example/ep",
            P256dh = "p256-key",
            Auth = "auth-key",
            IncludeUserChannel = true,
            IncludeAdminChannel = false
        };
        var response = new PushNotificationsController.VapidPublicKeyResponse("public-key", "mailto:ops@example.com");

        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = JsonSerializer.Serialize(response);

        const string expectedRequest = "{\"Endpoint\":\"https://push.example/ep\",\"P256dh\":\"p256-key\",\"Auth\":\"auth-key\",\"IncludeUserChannel\":true,\"IncludeAdminChannel\":false}";
        const string expectedResponse = "{\"PublicKey\":\"public-key\",\"Subject\":\"mailto:ops@example.com\"}";
        Assert.Equal(expectedRequest, requestJson);
        Assert.Equal(expectedResponse, responseJson);
    }
}
