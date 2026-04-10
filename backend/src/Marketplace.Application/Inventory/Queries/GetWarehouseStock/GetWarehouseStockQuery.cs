using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetWarehouseStock;

public sealed record GetWarehouseStockQuery(
    Guid CompanyId,
    long? WarehouseId,
    long? ProductId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<IReadOnlyList<WarehouseStockDto>>>;
