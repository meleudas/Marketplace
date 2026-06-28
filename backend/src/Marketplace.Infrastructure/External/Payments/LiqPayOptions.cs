namespace Marketplace.Infrastructure.External.Payments;

public sealed class LiqPayOptions
{
    public const string SectionName = "LiqPay";

    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://www.liqpay.ua/api";
    public string CheckoutBaseUrl { get; set; } = "https://www.liqpay.ua/api/3/checkout";
    public string CallbackBaseUrl { get; set; } = "https://localhost:7001";
    public string ResultBaseUrl { get; set; } = "http://localhost:3000";
}
