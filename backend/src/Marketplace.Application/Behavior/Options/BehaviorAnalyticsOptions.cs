namespace Marketplace.Application.Behavior.Options;

public sealed class BehaviorAnalyticsOptions
{
    public const string SectionName = "BehaviorAnalytics";

    public bool BehaviorTrackingEnabled { get; set; }
    public bool AdminAnalyticsReadEnabled { get; set; }
    public int DuplicateWindowSeconds { get; set; } = 30;
    public int PayloadMaxBytes { get; set; } = 8_192;
    public int RateLimitPerMinute { get; set; } = 120;
    public int SamplingPercent { get; set; } = 100;
    public bool RequireConsent { get; set; }
}
