namespace Marketplace.Application.Shipping.Options;

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    public bool Enabled { get; set; }
    public bool NovaPoshtaEnabled { get; set; }
}
