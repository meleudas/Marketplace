namespace Marketplace.API.OpenApi;

public static class OpenApiTagDefinitions
{
    public static IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Auth"] = "Реєстрація, логін, refresh та logout.",
        ["ExternalAuth"] = "OAuth-провайдери (Google).",
        ["Account"] = "Профіль поточного користувача, пароль, 2FA, email.",
        ["Users"] = "Адміністрування користувачів платформи.",
        ["Catalog"] = "Публічний каталог компаній, категорій і товарів.",
        ["Products"] = "CRUD товарів у workspace продавця.",
        ["Inventory"] = "Склади, залишки, резервування та рухи.",
        ["CompanyMembers"] = "Членство в компанії та компанійні ролі.",
        ["Cart"] = "Кошик покупця та checkout.",
        ["CartCoupons"] = "Застосування купонів до кошика.",
        ["Coupons"] = "Адміністрування купонів.",
        ["Orders"] = "Замовлення покупця, продавця та адміна.",
        ["Shipping"] = "Адреси, методи доставки та quote.",
        ["Shipments"] = "Відстеження відправлень покупця.",
        ["ShippingIntegrations"] = "Webhook-и перевізників (Nova Poshta).",
        ["Favorites"] = "Обрані товари користувача.",
        ["Reviews"] = "Відгуки на товари та компанії.",
        ["ReviewReplies"] = "Відповіді продавця на відгуки.",
        ["AdminReviews"] = "Модерація відгуків.",
        ["AdminCatalog"] = "Адмін-каталог: компанії, категорії, комісії.",
        ["AdminProducts"] = "Модерація товарів.",
        ["AdminPayments"] = "Адміністрування платежів.",
        ["AdminOutbox"] = "Перегляд outbox/dead-letter.",
        ["AdminReports"] = "Черга модерації скарг.",
        ["Reports"] = "Скарги користувачів.",
        ["Analytics"] = "Публічна/авторизована behavior analytics ingest.",
        ["AdminAnalytics"] = "KPI та зведення для адмінів.",
        ["PaymentsIntegrations"] = "Webhook LiqPay.",
        ["TelegramIntegrations"] = "Telegram bot webhook.",
        ["PushNotifications"] = "Web Push підписки та VAPID.",
        ["MeNotifications"] = "In-app нотифікації користувача.",
        ["Chats"] = "Чати buyer/seller/support та модерація.",
        ["Support"] = "Звернення до підтримки (helpdesk tickets).",
        ["AdminSupport"] = "Призначення, статуси та ескалація support tickets.",
        ["SupportIntegrations"] = "Inbound webhook helpdesk-провайдера."
    };

    public static string BuildDocumentDescription() =>
        """
        Канонічна документація endpoint-ів підтягується з `Docs/Endpoints/*.md`.

        Для кожної операції дивіться **Summary**, **Description** та custom extensions:
        `x-required-global-roles`, `x-frontend-status`, `x-notification-templates`, `x-idempotency-required`.

        Додатково: [RoleAccessMatrix](../Docs/DDD/RoleAccessMatrix.md), [EventCatalog](../Docs/Notifications/EventCatalog.md).
        """;
}
