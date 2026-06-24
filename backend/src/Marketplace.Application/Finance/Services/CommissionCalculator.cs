namespace Marketplace.Application.Finance.Services;

public sealed record CommissionCalculationResult(
    decimal MerchandiseSubtotal,
    decimal DiscountAmount,
    decimal MerchandiseBase,
    decimal CommissionPercent,
    decimal PlatformFee,
    decimal SellerMerchandiseNet,
    decimal ShippingAmount,
    decimal SellerPayoutEligible);

public static class CommissionCalculator
{
    public static CommissionCalculationResult Calculate(
        decimal subtotal,
        decimal discount,
        decimal shipping,
        decimal commissionPercent)
    {
        var merchandiseBase = subtotal - discount;
        var platformFee = Math.Round(merchandiseBase * commissionPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var sellerMerchandiseNet = merchandiseBase - platformFee;
        var sellerPayoutEligible = sellerMerchandiseNet + shipping;

        return new CommissionCalculationResult(
            subtotal,
            discount,
            merchandiseBase,
            commissionPercent,
            platformFee,
            sellerMerchandiseNet,
            shipping,
            sellerPayoutEligible);
    }
}
