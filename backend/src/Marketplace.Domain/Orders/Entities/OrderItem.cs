using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Orders.Entities;

public sealed class OrderItem : AuditableSoftDeleteAggregateRoot<OrderItemId>
{
    private OrderItem() { }

    public OrderId OrderId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductImage { get; private set; }
    public int Quantity { get; private set; }
    public Money PriceAtMoment { get; private set; } = Money.Zero;
    public Money Discount { get; private set; } = Money.Zero;
    public Money TotalPrice { get; private set; } = Money.Zero;
    public CompanyId CompanyId { get; private set; } = null!;

    public static OrderItem Reconstitute(
        OrderItemId id,
        OrderId orderId,
        ProductId productId,
        string productName,
        string? productImage,
        int quantity,
        Money priceAtMoment,
        Money discount,
        Money totalPrice,
        CompanyId companyId,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            ProductImage = productImage,
            Quantity = quantity,
            PriceAtMoment = priceAtMoment,
            Discount = discount,
            TotalPrice = totalPrice,
            CompanyId = companyId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
