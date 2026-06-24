namespace Marketplace.Application.Finance.Options;

public sealed class SettlementOptions
{
    public const string SectionName = "Settlement";

    public int PeriodDays { get; set; } = 7;
    public int HoldDaysAfterDelivery { get; set; } = 14;
    public bool AutoPayoutEnabled { get; set; }
}
