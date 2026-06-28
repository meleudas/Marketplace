using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.CreateUserAddress;

public sealed record CreateUserAddressCommand(
    Guid ActorUserId,
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
