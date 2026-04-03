using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Domain.Orders.Entities;

public sealed class Order : AuditableSoftDeleteAggregateRoot<OrderId>
{
    private Order() { }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public CompanyId CompanyId { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero;
    public Money Subtotal { get; private set; } = Money.Zero;
    public Money ShippingCost { get; private set; } = Money.Zero;
    public Money DiscountAmount { get; private set; } = Money.Zero;
    public Money TaxAmount { get; private set; } = Money.Zero;
    public ShippingMethodId ShippingMethodId { get; private set; } = null!;
    public CheckoutPaymentMethod PaymentMethod { get; private set; }
    public string? Notes { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    public static Order Reconstitute(
        OrderId id,
        string orderNumber,
        Guid customerId,
        CompanyId companyId,
        OrderStatus status,
        Money totalPrice,
        Money subtotal,
        Money shippingCost,
        Money discountAmount,
        Money taxAmount,
        ShippingMethodId shippingMethodId,
        CheckoutPaymentMethod paymentMethod,
        string? notes,
        string? trackingNumber,
        DateTime? shippedAt,
        DateTime? deliveredAt,
        DateTime? cancelledAt,
        DateTime? refundedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            CompanyId = companyId,
            Status = status,
            TotalPrice = totalPrice,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            ShippingMethodId = shippingMethodId,
            PaymentMethod = paymentMethod,
            Notes = notes,
            TrackingNumber = trackingNumber,
            ShippedAt = shippedAt,
            DeliveredAt = deliveredAt,
            CancelledAt = cancelledAt,
            RefundedAt = refundedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
