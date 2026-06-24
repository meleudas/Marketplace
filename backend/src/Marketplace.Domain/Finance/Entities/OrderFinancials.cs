using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Finance.Entities;

public sealed class OrderFinancials : AggregateRoot<OrderFinancialsId>
{
    private OrderFinancials() { }

    public OrderId OrderId { get; private set; } = null!;
    public PaymentId PaymentId { get; private set; } = null!;
    public CompanyId CompanyId { get; private set; } = null!;
    public string Currency { get; private set; } = "UAH";
    public decimal MerchandiseSubtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal MerchandiseBase { get; private set; }
    public decimal CommissionPercent { get; private set; }
    public decimal PlatformFee { get; private set; }
    public decimal SellerMerchandiseNet { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal SellerPayoutEligible { get; private set; }
    public DateTime PostedAtUtc { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static OrderFinancials Create(
        OrderFinancialsId id,
        OrderId orderId,
        PaymentId paymentId,
        CompanyId companyId,
        string currency,
        decimal merchandiseSubtotal,
        decimal discountAmount,
        decimal merchandiseBase,
        decimal commissionPercent,
        decimal platformFee,
        decimal sellerMerchandiseNet,
        decimal shippingAmount,
        decimal sellerPayoutEligible,
        DateTime postedAtUtc)
    {
        if (merchandiseBase < 0)
            throw new DomainException("Merchandise base cannot be negative");

        var now = DateTime.UtcNow;
        return new OrderFinancials
        {
            Id = id,
            OrderId = orderId,
            PaymentId = paymentId,
            CompanyId = companyId,
            Currency = string.IsNullOrWhiteSpace(currency) ? "UAH" : currency.Trim().ToUpperInvariant(),
            MerchandiseSubtotal = merchandiseSubtotal,
            DiscountAmount = discountAmount,
            MerchandiseBase = merchandiseBase,
            CommissionPercent = commissionPercent,
            PlatformFee = platformFee,
            SellerMerchandiseNet = sellerMerchandiseNet,
            ShippingAmount = shippingAmount,
            SellerPayoutEligible = sellerPayoutEligible,
            PostedAtUtc = postedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static OrderFinancials Reconstitute(
        OrderFinancialsId id,
        OrderId orderId,
        PaymentId paymentId,
        CompanyId companyId,
        string currency,
        decimal merchandiseSubtotal,
        decimal discountAmount,
        decimal merchandiseBase,
        decimal commissionPercent,
        decimal platformFee,
        decimal sellerMerchandiseNet,
        decimal shippingAmount,
        decimal sellerPayoutEligible,
        DateTime postedAtUtc,
        DateTime createdAt,
        DateTime updatedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            PaymentId = paymentId,
            CompanyId = companyId,
            Currency = currency,
            MerchandiseSubtotal = merchandiseSubtotal,
            DiscountAmount = discountAmount,
            MerchandiseBase = merchandiseBase,
            CommissionPercent = commissionPercent,
            PlatformFee = platformFee,
            SellerMerchandiseNet = sellerMerchandiseNet,
            ShippingAmount = shippingAmount,
            SellerPayoutEligible = sellerPayoutEligible,
            PostedAtUtc = postedAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}
