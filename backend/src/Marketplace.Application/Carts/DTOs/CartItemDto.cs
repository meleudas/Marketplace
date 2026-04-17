namespace Marketplace.Application.Carts.DTOs;

public sealed record CartItemDto(
    long Id,
    long ProductId,
    int Quantity,
    decimal PriceAtMoment,
    decimal Discount,
    decimal LineTotal);
