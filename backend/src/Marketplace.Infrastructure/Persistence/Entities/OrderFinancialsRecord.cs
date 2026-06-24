namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class OrderFinancialsRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long PaymentId { get; set; }
    public Guid CompanyId { get; set; }
    public string Currency { get; set; } = "UAH";
    public decimal MerchandiseSubtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MerchandiseBase { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal SellerMerchandiseNet { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal SellerPayoutEligible { get; set; }
    public DateTime PostedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
