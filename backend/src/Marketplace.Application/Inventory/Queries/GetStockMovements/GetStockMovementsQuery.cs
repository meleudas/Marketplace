using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery(
    Guid CompanyId,
    long? ProductId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<IReadOnlyList<StockMovementDto>>>;
