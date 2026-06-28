using System.Net;
using System.Net.Http.Headers;
using Marketplace.Domain.Notifications.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Orders;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Orders")]
[Trait("Suite", "Notifications")]
[Trait("Layer", "E2E")]
public sealed class OrderInAppNotificationE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public OrderInAppNotificationE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_InApp_Notifications_Returns_Inserted_Row()
    {
        var (client, userId) = await _fixture.CreateAuthenticatedClientAsync();

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var repo = new InAppNotificationRepository(db);
            await repo.TryInsertAsync(
                userId,
                NotificationKind.Order,
                "Статус",
                "Замовлення в дорозі",
                "{\"templateKey\":\"UserOrderStatus\",\"orderId\":99}",
                "/orders/99",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(1),
                null,
                CancellationToken.None);
        }

        var response = await client.GetAsync("/me/in-app-notifications?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("UserOrderStatus", body);
    }
}
