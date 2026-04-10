namespace Marketplace.Application.Inventory.DTOs;

public sealed record WarehouseDto(
    long Id,
    Guid CompanyId,
    string Name,
    string Code,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string TimeZone,
    int Priority,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
