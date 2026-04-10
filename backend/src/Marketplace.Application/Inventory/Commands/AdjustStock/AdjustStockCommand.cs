using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.AdjustStock;

public sealed record AdjustStockCommand(
    Guid CompanyId,
    long WarehouseId,
    long ProductId,
    int OnHand,
    int Reserved,
    int ReorderPoint,
    string OperationId,
    string Reason,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<WarehouseStockDto>>;
