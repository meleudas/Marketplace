namespace Marketplace.Application.Inventory.DTOs;

public sealed record WarehouseStockDto(
    long Id,
    Guid CompanyId,
    long WarehouseId,
    long ProductId,
    int OnHand,
    int Reserved,
    int Available,
    int ReorderPoint,
    long Version,
    DateTime UpdatedAt);
