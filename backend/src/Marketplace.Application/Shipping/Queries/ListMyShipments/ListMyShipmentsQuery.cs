using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.ListMyShipments;

public sealed record ListMyShipmentsQuery(Guid ActorUserId) : IRequest<Result<IReadOnlyList<ShipmentDto>>>;
