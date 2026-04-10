using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReceiveStock;

public sealed record ReceiveStockCommand(
    Guid CompanyId,
    long WarehouseId,
    long ProductId,
    int Quantity,
    string OperationId,
    string? Reference,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<WarehouseStockDto>>;
