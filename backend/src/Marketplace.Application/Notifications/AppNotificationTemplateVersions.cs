namespace Marketplace.Application.Notifications;

public static class AppNotificationTemplateVersions
{
  private static readonly IReadOnlyDictionary<string, int> Versions = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [AppNotificationTemplateKeys.AdminNewOrder] = 1,
        [AppNotificationTemplateKeys.CompanyNewOrder] = 1,
        [AppNotificationTemplateKeys.UserOrderStatus] = 2,
        [AppNotificationTemplateKeys.UserPaymentStatus] = 1,
        [AppNotificationTemplateKeys.CartProductBackInStock] = 1,
        [AppNotificationTemplateKeys.AdminProductPendingReview] = 1,
        [AppNotificationTemplateKeys.UserProductApproved] = 1,
        [AppNotificationTemplateKeys.UserProductRejected] = 1,
        [AppNotificationTemplateKeys.ChatMessageReceived] = 1,
        [AppNotificationTemplateKeys.SupportTicketStatusChanged] = 1,
    };

    public static int GetVersion(string templateKey) =>
        Versions.TryGetValue(templateKey, out var version) ? version : 1;
}
