using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Cart.Entities;

namespace Marketplace.Application.Carts.Mappings;

internal static class CartMapping
{
    public static CartDto ToDto(Cart cart, IReadOnlyList<CartItem> items)
    {
        var dtoItems = items
            .Select(x =>
            {
                var lineTotal = (x.PriceAtMoment.Amount - x.Discount.Amount) * x.Quantity;
                return new CartItemDto(
                    x.Id.Value,
                    x.ProductId.Value,
                    x.Quantity,
                    x.PriceAtMoment.Amount,
                    x.Discount.Amount,
                    lineTotal);
            })
            .ToList();

        return new CartDto(
            cart.Id.Value,
            cart.UserId,
            cart.LastActivityAt,
            dtoItems,
            dtoItems.Sum(x => x.Quantity),
            dtoItems.Sum(x => x.LineTotal));
    }
}
