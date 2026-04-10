using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReserveStock;

public sealed record ReserveStockCommand(
    Guid CompanyId,
    long WarehouseId,
    long ProductId,
    int Quantity,
    string ReservationCode,
    int TtlMinutes,
    string? Reference,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result>;
