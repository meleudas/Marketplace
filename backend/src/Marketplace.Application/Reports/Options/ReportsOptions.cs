namespace Marketplace.Application.Reports.Options;

public sealed class ReportsOptions
{
    public const string SectionName = "Reports";

    public bool PublicCreateEnabled { get; set; }
    public bool ModerationEnabled { get; set; }
    public int DuplicateCooldownMinutes { get; set; } = 10;
    public int RateLimitPerWindow { get; set; } = 5;
    public int RateLimitWindowMinutes { get; set; } = 1;
}
