namespace Marketplace.Infrastructure.External.Analytics;

public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";

    public bool Enabled { get; set; }
    public string Url { get; set; } = "http://localhost:8123";
    public string Database { get; set; } = "marketplace";
    public string Username { get; set; } = "default";
    public string Password { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int SignalLookbackDays { get; set; } = 30;
    public int SignalHalfLifeDays { get; set; } = 30;
    public int FunnelLookbackDays { get; set; } = 30;
    public double ViewWeight { get; set; } = 1;
    public double SearchWeight { get; set; } = 2;
    public double FavoriteWeight { get; set; } = 4;
    public double CartWeight { get; set; } = 6;
    public double PurchaseWeight { get; set; } = 10;
}
