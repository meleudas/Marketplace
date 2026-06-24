namespace Marketplace.Domain.Shipping.Entities;

public sealed record ShipmentFulfillmentSummary(
    int TotalOrderItems,
    int FullyShippedItems,
    int FullyDeliveredItems,
    bool IsFullyShipped,
    bool IsFullyDelivered);
