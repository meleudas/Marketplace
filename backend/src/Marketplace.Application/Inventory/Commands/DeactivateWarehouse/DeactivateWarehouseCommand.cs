using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.DeactivateWarehouse;

public sealed record DeactivateWarehouseCommand(
    Guid CompanyId,
    long WarehouseId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result>;
