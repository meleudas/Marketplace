namespace Marketplace.Tests.Fixtures;

internal static class E2ESharedSettings
{
    public static Dictionary<string, string?> Build(
        string postgresConnectionString,
        string redisConnectionString,
        string elasticsearchUrl,
        string minioEndpoint) => new()
    {
        ["ConnectionStrings:Database"] = postgresConnectionString,
        ["ConnectionStrings:Redis"] = redisConnectionString,
        ["Elasticsearch:Enabled"] = "true",
        ["Elasticsearch:Url"] = elasticsearchUrl,
        ["Storage:Enabled"] = "true",
        ["Storage:Endpoint"] = minioEndpoint,
        ["Storage:AccessKey"] = "minioadmin",
        ["Storage:SecretKey"] = "minioadmin",
        ["Storage:Bucket"] = "marketplace-media",
        ["Storage:UseSsl"] = "false",
        ["Storage:PublicBaseUrl"] = "http://localhost:9000",
        ["Jwt:SecretKey"] = "E2E_Test_SecretKey_AtLeast32CharsLong!!",
        ["Jwt:Issuer"] = "marketplace-e2e",
        ["Jwt:Audience"] = "marketplace-e2e",
        ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
        ["Frontend:BaseUrl"] = "http://localhost:3000",
        ["LiqPay:PublicKey"] = "sandbox_test",
        ["LiqPay:PrivateKey"] = "sandbox_test",
        ["Telegram:WebhookSecret"] = "e2e-telegram-secret",
        ["WebPush:Enabled"] = "false",
        ["AppNotifications:EmailEnabled"] = "false",
        ["AppNotifications:TelegramEnabled"] = "false",
        ["Identity:RequireConfirmedEmail"] = "false",
        ["Shipping:Enabled"] = "true",
        ["Shipping:NovaPoshtaEnabled"] = "true",
        ["NovaPoshta:Enabled"] = "true",
        ["Coupons:ReadEnabled"] = "true",
        ["Coupons:CheckoutConsumeEnabled"] = "true",
        ["Chats:Enabled"] = "true",
        ["Chats:RealtimeEnabled"] = "false",
        ["Chats:ModerationEnabled"] = "false",
        ["Chats:RejectOnProhibitedContent"] = "false",
        ["BehaviorAnalytics:BehaviorTrackingEnabled"] = "true",
        ["ClickHouse:Enabled"] = "false",
        ["RateLimiting:Enabled"] = "false",
    };
}
