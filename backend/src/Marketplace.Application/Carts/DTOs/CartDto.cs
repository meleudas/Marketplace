namespace Marketplace.Application.Carts.DTOs;

public sealed record CartDto(
    long Id,
    Guid UserId,
    DateTime LastActivityAt,
    IReadOnlyList<CartItemDto> Items,
    int TotalItems,
    decimal TotalAmount);
