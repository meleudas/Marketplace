namespace Marketplace.Application.Support.Options;

public sealed class SupportOptions
{
    public const string SectionName = "Support";

    public bool Enabled { get; set; }
    public bool HelpdeskSyncEnabled { get; set; }
    public bool HelpdeskWebhookEnabled { get; set; }
    public int CreateRateLimitPerWindow { get; set; } = 5;
    public int CreateRateLimitWindowMinutes { get; set; } = 60;
    public int SlaHoursP1 { get; set; } = 4;
    public int SlaHoursP2 { get; set; } = 24;
    public string WebhookSigningSecret { get; set; } = string.Empty;
    public string HelpdeskProvider { get; set; } = "logging";
}
