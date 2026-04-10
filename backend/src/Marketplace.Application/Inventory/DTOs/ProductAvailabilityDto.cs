namespace Marketplace.Application.Inventory.DTOs;

public sealed record ProductAvailabilityDto(
    long ProductId,
    int AvailableQty,
    string AvailabilityStatus);
