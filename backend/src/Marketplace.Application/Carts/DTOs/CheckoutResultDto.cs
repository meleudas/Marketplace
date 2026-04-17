using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Application.Carts.DTOs;

public sealed record CheckoutResultDto(IReadOnlyList<CreatedOrderDto> CreatedOrders);

public sealed record CreatedOrderDto(
    long OrderId,
    string OrderNumber,
    Guid CompanyId,
    OrderStatus Status,
    int ItemCount,
    decimal TotalPrice);
