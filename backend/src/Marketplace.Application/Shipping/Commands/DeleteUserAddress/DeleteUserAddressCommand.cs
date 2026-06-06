using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.DeleteUserAddress;

public sealed record DeleteUserAddressCommand(Guid ActorUserId, long AddressId) : IRequest<Result>;
