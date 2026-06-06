namespace Marketplace.Infrastructure.External.Shipping;

public sealed class NovaPoshtaOptions
{
    public const string SectionName = "NovaPoshta";

    public bool Enabled { get; set; }
    public string ApiUrl { get; set; } = "https://api.novaposhta.ua/v2.0/json/";
    public string ApiKey { get; set; } = string.Empty;
    public decimal FallbackFlatRate { get; set; } = 99m;
    public int FallbackEtaMinDays { get; set; } = 1;
    public int FallbackEtaMaxDays { get; set; } = 3;
}
