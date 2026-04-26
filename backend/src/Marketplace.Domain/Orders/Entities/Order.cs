using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Common.Exceptions;
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

    public void MarkPaid()
    {
        EnsureNotDeleted();
        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        EnsureNotDeleted();
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        EnsureNotDeleted();
        Status = OrderStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProcessing()
    {
        EnsureNotDeleted();
        if (Status is not (OrderStatus.Pending or OrderStatus.Paid))
            throw new DomainException("Invalid status transition");
        Status = OrderStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetShipped(string? trackingNumber)
    {
        EnsureNotDeleted();
        if (Status != OrderStatus.Processing)
            throw new DomainException("Invalid status transition");
        Status = OrderStatus.Shipped;
        TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? TrackingNumber : trackingNumber.Trim();
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDelivered()
    {
        EnsureNotDeleted();
        if (Status != OrderStatus.Shipped)
            throw new DomainException("Invalid status transition");
        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        EnsureNotDeleted();
        if (Status is OrderStatus.Delivered or OrderStatus.Refunded or OrderStatus.Cancelled or OrderStatus.Shipped)
            throw new DomainException("Invalid status transition");
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted order");
    }
}
