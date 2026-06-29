namespace Marketplace.Application.Payments.Options;

public sealed class PaymentWebhookAntiAbuseOptions
{
    public const string SectionName = "PaymentWebhookAntiAbuse";

    public int FailedSignaturePerIpPerWindow { get; set; } = 20;
    public int FailedSignatureWindowMinutes { get; set; } = 5;
}
