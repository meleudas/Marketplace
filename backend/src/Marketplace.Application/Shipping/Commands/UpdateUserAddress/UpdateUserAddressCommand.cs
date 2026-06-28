using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.UpdateUserAddress;

public sealed record UpdateUserAddressCommand(
    Guid ActorUserId,
    long AddressId,
    string Type,
    bool IsDefault,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country) : IRequest<Result<UserAddressDto>>;
