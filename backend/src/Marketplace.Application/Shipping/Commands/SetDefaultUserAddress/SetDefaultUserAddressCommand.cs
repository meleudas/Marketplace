using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.SetDefaultUserAddress;

public sealed record SetDefaultUserAddressCommand(Guid ActorUserId, long AddressId) : IRequest<Result>;
