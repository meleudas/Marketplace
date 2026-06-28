namespace Marketplace.Application.Coupons.Validation;

public sealed record CouponCartLine(
    long ProductId,
    long CategoryId,
    Guid CompanyId,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
