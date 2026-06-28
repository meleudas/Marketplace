namespace Marketplace.Application.Notifications;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    public bool Enabled { get; set; }
    public string Subject { get; set; } = "mailto:dev@localhost";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
