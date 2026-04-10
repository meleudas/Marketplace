using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.TransferStock;

public sealed record TransferStockCommand(
    Guid CompanyId,
    long FromWarehouseId,
    long ToWarehouseId,
    long ProductId,
    int Quantity,
    string OperationId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result>;
