using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Inventory.Entities;

namespace Marketplace.Application.Inventory.Mappings;

public static class InventoryMapper
{
    public static WarehouseDto ToDto(Warehouse x) =>
        new(
            x.Id.Value,
            x.CompanyId.Value,
            x.Name,
            x.Code,
            x.Address.Street,
            x.Address.City,
            x.Address.State,
            x.Address.PostalCode,
            x.Address.Country,
            x.TimeZone,
            x.Priority,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt);

    public static WarehouseStockDto ToDto(WarehouseStock x) =>
        new(
            x.Id.Value,
            x.CompanyId.Value,
            x.WarehouseId.Value,
            x.ProductId.Value,
            x.OnHand,
            x.Reserved,
            x.Available,
            x.ReorderPoint,
            x.Version,
            x.UpdatedAt);

    public static StockMovementDto ToDto(StockMovement x) =>
        new(
            x.Id.Value,
            x.CompanyId.Value,
            x.WarehouseId.Value,
            x.ProductId.Value,
            x.Type.ToString(),
            x.Quantity,
            x.OperationId,
            x.Reference,
            x.Reason,
            x.ActorUserId,
            x.OccurredAt);
}
