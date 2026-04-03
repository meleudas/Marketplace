using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Cart.Entities;

public sealed class CartItem : AuditableSoftDeleteAggregateRoot<CartItemId>
{
    private CartItem() { }

    public CartId CartId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money PriceAtMoment { get; private set; } = Money.Zero;
    public Money Discount { get; private set; } = Money.Zero;

    public static CartItem Reconstitute(
        CartItemId id,
        CartId cartId,
        ProductId productId,
        int quantity,
        Money priceAtMoment,
        Money discount,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity,
            PriceAtMoment = priceAtMoment,
            Discount = discount,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
