using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.CalculateShippingQuote;

public sealed record CalculateShippingQuoteCommand(
    Guid ActorUserId,
    long ShippingMethodId,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country) : IRequest<Result<ShippingQuoteDto>>;
