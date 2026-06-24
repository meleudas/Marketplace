namespace Marketplace.Application.Common.Options;

public sealed class IntegrationRetryOptions
{
    public const string SectionName = "IntegrationRetry";

    public int MaxAttempts { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
    public int MaxBackoffMinutes { get; set; } = 60;
    public int BaseBackoffMinutes { get; set; } = 1;
}
