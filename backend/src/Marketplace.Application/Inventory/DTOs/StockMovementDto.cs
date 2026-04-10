namespace Marketplace.Application.Inventory.DTOs;

public sealed record StockMovementDto(
    long Id,
    Guid CompanyId,
    long WarehouseId,
    long ProductId,
    string Type,
    int Quantity,
    string OperationId,
    string? Reference,
    string? Reason,
    Guid ActorUserId,
    DateTime OccurredAt);
