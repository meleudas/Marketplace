using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.ListMyAddresses;

public sealed record ListMyAddressesQuery(Guid ActorUserId) : IRequest<Result<IReadOnlyList<UserAddressDto>>>;
