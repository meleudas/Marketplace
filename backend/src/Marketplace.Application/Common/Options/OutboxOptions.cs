namespace Marketplace.Application.Common.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int MaxAttempts { get; set; } = 10;
    public int BatchSize { get; set; } = 100;
    public int MaxBackoffMinutes { get; set; } = 60;
    public int BaseBackoffMinutes { get; set; } = 1;
}
