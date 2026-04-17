using Marketplace.Application.Carts.DTOs;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.CheckoutCart;

public sealed record CheckoutCartCommand(
    Guid ActorUserId,
    CheckoutPaymentMethod PaymentMethod,
    CheckoutAddressDto Address,
    string? Notes) : IRequest<Result<CheckoutResultDto>>;

public sealed record CheckoutAddressDto(
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
