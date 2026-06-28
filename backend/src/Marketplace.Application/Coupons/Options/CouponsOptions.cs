namespace Marketplace.Application.Coupons.Options;

public sealed class CouponsOptions
{
    public const string SectionName = "Coupons";
    public bool ReadEnabled { get; set; } = true;
    public bool CheckoutConsumeEnabled { get; set; } = true;
}
