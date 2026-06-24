namespace Marketplace.Application.Shipping.DTOs;

public sealed record UserAddressDto(
    long Id,
    string Type,
    bool IsDefault,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public sealed record ShippingMethodDto(
    long Id,
    string Name,
    string CarrierCode,
    decimal Price,
    decimal? FreeShippingThreshold,
    int EstimatedDaysMin,
    int EstimatedDaysMax);

public sealed record ShippingQuoteDto(
    long QuoteId,
    long ShippingMethodId,
    decimal Amount,
    int EstimatedDaysMin,
    int EstimatedDaysMax,
    DateTime ExpiresAtUtc);

public sealed record ShipmentDto(
    long Id,
    long OrderId,
    int ShipmentNumber,
    long ShippingMethodId,
    string CarrierCode,
    string Status,
    string? TrackingNumber,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ShipmentSummaryDto(
    long Id,
    long OrderId,
    int ShipmentNumber,
    string Status,
    string? TrackingNumber,
    int TotalQuantity,
    DateTime CreatedAt);

public sealed record ShipmentItemDto(long OrderItemId, int Quantity);

public sealed record ShippingEventDto(
    string CarrierCode,
    string EventKey,
    string? TrackingNumber,
    string? DeliveryStatus,
    DateTime OccurredAtUtc);

public sealed record ShipmentDetailDto(
    long Id,
    long OrderId,
    int ShipmentNumber,
    long ShippingMethodId,
    string CarrierCode,
    string Status,
    string? TrackingNumber,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ShipmentItemDto> Items,
    IReadOnlyList<ShippingEventDto> Events);

public sealed record WarehouseFulfillmentGroupDto(
    long WarehouseId,
    string WarehouseName,
    IReadOnlyList<PendingShipmentItemDto> Items);

public sealed record FulfillmentReadinessDto(
    int TotalOrderItems,
    int FullyShippedItems,
    int FullyDeliveredItems,
    bool IsFullyShipped,
    bool IsFullyDelivered,
    IReadOnlyList<PendingShipmentItemDto> PendingItems,
    IReadOnlyList<WarehouseFulfillmentGroupDto> PendingByWarehouse,
    IReadOnlyList<ShipmentSummaryDto> Shipments);

public sealed record PendingShipmentItemDto(long OrderItemId, int OrderedQuantity, int ShippedQuantity, int RemainingQuantity);
