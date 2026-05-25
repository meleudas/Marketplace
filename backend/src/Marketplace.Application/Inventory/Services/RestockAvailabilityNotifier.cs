using System.Globalization;
using System.Text.Json;
using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Inventory.Services;

public sealed class RestockAvailabilityNotifier : IRestockAvailabilityNotifier
{
    public static readonly TimeSpan MinIntervalBetweenNotifications = TimeSpan.FromHours(24);

    private readonly ICartStockWatchRepository _watches;
    private readonly IProductRepository _products;
    private readonly IAppNotificationScheduler _appNotifications;

    public RestockAvailabilityNotifier(
        ICartStockWatchRepository watches,
        IProductRepository products,
        IAppNotificationScheduler appNotifications)
    {
        _watches = watches;
        _products = products;
        _appNotifications = appNotifications;
    }

    public async Task NotifyIfCrossedFromZeroAsync(
        Guid companyId,
        long productId,
        int beforeAvailableSum,
        int afterAvailableSum,
        CancellationToken ct = default)
    {
        if (beforeAvailableSum != 0 || afterAvailableSum <= 0)
            return;

        var product = await _products.GetByIdAsync(ProductId.From(productId), ct);
        if (product is null || product.IsDeleted || product.CompanyId.Value != companyId)
            return;

        var utcNow = DateTime.UtcNow;
        var userIds = await _watches.ListUserIdsEligibleForNotifyAsync(
            productId,
            MinIntervalBetweenNotifications,
            utcNow,
            ct);
        if (userIds.Count == 0)
            return;

        var dateBucket = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var payloadJson = JsonSerializer.Serialize(new
        {
            productId = product.Id.Value,
            productName = product.Name,
            slug = product.Slug,
            companyId = product.CompanyId.Value
        });

        foreach (var userId in userIds)
        {
            await _appNotifications.ScheduleAsync(
                new AppNotificationRequest
                {
                    TemplateKey = AppNotificationTemplateKeys.CartProductBackInStock,
                    CorrelationId = AppNotificationCorrelationIds.CartRestockNotify(userId, productId, dateBucket),
                    Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                    Audience = AppNotificationAudienceKind.User,
                    TargetUserId = userId,
                    PayloadJson = payloadJson
                },
                ct);
            await _watches.TouchLastNotifiedAsync(userId, productId, utcNow, ct);
        }
    }
}
