using System.Text.Json;
using Marketplace.Application.Notifications;
using Marketplace.Infrastructure;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Notifications;

public sealed class AppNotificationPayloadBuilder
{
    private readonly IOptionsMonitor<FrontendOptions> _frontend;

    public AppNotificationPayloadBuilder(IOptionsMonitor<FrontendOptions> frontend)
    {
        _frontend = frontend;
    }

    public AppNotificationEnvelope Build(AppNotificationRequest request)
    {
        var baseUrl = (_frontend.CurrentValue.BaseUrl ?? string.Empty).TrimEnd('/');
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson);
        var root = doc.RootElement;

        return request.TemplateKey switch
        {
            AppNotificationTemplateKeys.AdminNewOrder => BuildAdminNewOrder(request, root, baseUrl),
            AppNotificationTemplateKeys.CompanyNewOrder => BuildCompanyNewOrder(request, root, baseUrl),
            AppNotificationTemplateKeys.UserOrderStatus => BuildUserOrderStatus(request, root, baseUrl),
            AppNotificationTemplateKeys.UserPaymentStatus => BuildUserPaymentStatus(request, root, baseUrl),
            AppNotificationTemplateKeys.CartProductBackInStock => BuildCartProductBackInStock(request, root, baseUrl),
            AppNotificationTemplateKeys.AdminProductPendingReview => BuildAdminProductPendingReview(request, root, baseUrl),
            AppNotificationTemplateKeys.UserProductApproved => BuildUserProductApproved(request, root, baseUrl),
            AppNotificationTemplateKeys.UserProductRejected => BuildUserProductRejected(request, root, baseUrl),
            AppNotificationTemplateKeys.ChatMessageReceived => BuildChatMessageReceived(request, root, baseUrl),
            AppNotificationTemplateKeys.SupportTicketStatusChanged => BuildSupportTicketStatusChanged(request, root, baseUrl),
            _ => new AppNotificationEnvelope
            {
                TemplateKey = request.TemplateKey,
                CorrelationId = request.CorrelationId,
                Channels = request.Channels,
                Audience = request.Audience,
                TargetUserId = request.TargetUserId,
                TargetCompanyId = request.TargetCompanyId,
                Title = "Marketplace",
                Body = request.TemplateKey,
                ActionUrl = baseUrl.Length > 0 ? baseUrl : null,
                PayloadJson = request.PayloadJson
            }
        };
    }

    private static AppNotificationEnvelope BuildAdminNewOrder(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var orderId = TryGetLong(root, "orderId");
        var orderNumber = TryGetString(root, "orderNumber") ?? "";
        var title = "Нове замовлення";
        var body = string.IsNullOrEmpty(orderNumber) ? $"Замовлення #{orderId}" : $"Замовлення {orderNumber}";
        var action = orderId is null || baseUrl.Length == 0 ? null : $"{baseUrl}/admin/orders/{orderId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildCompanyNewOrder(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var orderId = TryGetLong(root, "orderId");
        var orderNumber = TryGetString(root, "orderNumber") ?? "";
        var companyId = TryGetGuid(root, "companyId");
        var title = "Нове замовлення у компанії";
        var body = string.IsNullOrEmpty(orderNumber) ? $"Замовлення #{orderId}" : $"Замовлення {orderNumber}";
        string? action = null;
        if (orderId is not null && companyId is not null && baseUrl.Length > 0)
            action = $"{baseUrl}/companies/{companyId:D}/orders/{orderId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildUserOrderStatus(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var orderId = TryGetLong(root, "orderId");
        var orderNumber = TryGetString(root, "orderNumber");
        var status = TryGetString(root, "status") ?? "";
        var title = "Статус замовлення";
        var body = string.IsNullOrEmpty(orderNumber)
            ? $"Замовлення #{orderId}: {status}"
            : $"Замовлення {orderNumber}: {status}";
        var action = orderId is null || baseUrl.Length == 0 ? null : $"{baseUrl}/orders/{orderId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildAdminProductPendingReview(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var productId = TryGetLong(root, "productId");
        var name = TryGetString(root, "name") ?? "Товар";
        var slug = TryGetString(root, "slug");
        var title = "Новий товар на модерацію";
        var body = string.IsNullOrWhiteSpace(name) ? $"Товар #{productId}" : name;
        string? action = null;
        if (baseUrl.Length > 0)
            action = $"{baseUrl}/admin/products/pending";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildUserProductApproved(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var productId = TryGetLong(root, "productId");
        var name = TryGetString(root, "name") ?? "Товар";
        var slug = TryGetString(root, "slug");
        var title = "Товар схвалено";
        var body = $"«{name}» опубліковано в каталозі.";
        string? action = null;
        if (baseUrl.Length > 0 && !string.IsNullOrWhiteSpace(slug))
            action = $"{baseUrl}/products/{slug}";
        else if (baseUrl.Length > 0 && productId is not null)
            action = $"{baseUrl}/products/{productId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildUserProductRejected(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var productId = TryGetLong(root, "productId");
        var name = TryGetString(root, "name") ?? "Товар";
        var reason = TryGetString(root, "reason");
        var companyId = TryGetGuid(root, "companyId");
        var title = "Товар не схвалено";
        var body = string.IsNullOrWhiteSpace(reason)
            ? $"«{name}» повернуто до чернетки. Внесіть правки та надішліть знову."
            : $"«{name}»: {reason}";
        string? action = null;
        if (baseUrl.Length > 0 && companyId is not null && productId is not null)
            action = $"{baseUrl}/companies/{companyId:D}/products/{productId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildCartProductBackInStock(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var productId = TryGetLong(root, "productId");
        var productName = TryGetString(root, "productName") ?? "Товар";
        var slug = TryGetString(root, "slug");
        var title = "Товар знову в наявності";
        var body = $"{productName} знову можна додати до кошика.";
        string? action = null;
        if (baseUrl.Length > 0 && !string.IsNullOrWhiteSpace(slug))
            action = $"{baseUrl}/products/{slug}";
        else if (baseUrl.Length > 0 && productId is not null)
            action = $"{baseUrl}/products/{productId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildUserPaymentStatus(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var orderId = TryGetLong(root, "orderId");
        var orderNumber = TryGetString(root, "orderNumber");
        var paymentStatus = TryGetString(root, "paymentStatus") ?? "";
        var orderStatus = TryGetString(root, "orderStatus") ?? "";
        var title = "Оплата замовлення";
        var body = string.IsNullOrEmpty(orderNumber)
            ? $"Замовлення #{orderId}: {paymentStatus}"
            : $"Замовлення {orderNumber}: {paymentStatus}";
        if (!string.IsNullOrEmpty(orderStatus))
            body += $" ({orderStatus})";
        var action = orderId is null || baseUrl.Length == 0 ? null : $"{baseUrl}/orders/{orderId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildChatMessageReceived(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var chatId = TryGetGuid(root, "chatId");
        var preview = TryGetString(root, "preview") ?? "Нове повідомлення";
        var title = "Нове повідомлення в чаті";
        var action = chatId is null || baseUrl.Length == 0 ? null : $"{baseUrl}/chats/{chatId:D}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = preview,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static AppNotificationEnvelope BuildSupportTicketStatusChanged(
        AppNotificationRequest request,
        JsonElement root,
        string baseUrl)
    {
        var ticketId = TryGetLong(root, "ticketId");
        var ticketNumber = TryGetString(root, "ticketNumber") ?? "";
        var status = TryGetLong(root, "status");
        var title = "Оновлення звернення підтримки";
        var body = string.IsNullOrEmpty(ticketNumber)
            ? $"Статус звернення #{ticketId} змінено"
            : $"Звернення {ticketNumber}: новий статус {status}";
        var action = ticketId is null || baseUrl.Length == 0 ? null : $"{baseUrl}/support/tickets/{ticketId}";
        return new AppNotificationEnvelope
        {
            TemplateKey = request.TemplateKey,
            TemplateVersion = AppNotificationTemplateVersions.GetVersion(request.TemplateKey),
            CorrelationId = request.CorrelationId,
            Channels = request.Channels,
            Audience = request.Audience,
            TargetUserId = request.TargetUserId,
            TargetCompanyId = request.TargetCompanyId,
            Title = title,
            Body = body,
            ActionUrl = action,
            PayloadJson = request.PayloadJson
        };
    }

    private static long? TryGetLong(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) && p.TryGetInt64(out var v) ? v : null;

    private static string? TryGetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) ? p.GetString() : null;

    private static Guid? TryGetGuid(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) && p.TryGetGuid(out var g) ? g : null;
}
