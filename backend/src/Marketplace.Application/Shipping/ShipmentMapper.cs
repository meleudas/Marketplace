using Marketplace.Domain.Shipping.Entities;
using Marketplace.Application.Shipping.DTOs;

namespace Marketplace.Application.Shipping;

internal static class ShipmentMapper
{
    public static ShipmentDetailDto ToDetail(
        Shipment shipment,
        IReadOnlyList<ShipmentItem> items,
        IReadOnlyList<ShippingEventDto> events) =>
        new(
            shipment.Id.Value,
            shipment.OrderId.Value,
            shipment.ShipmentNumber,
            shipment.ShippingMethodId.Value,
            shipment.CarrierCode.ToString(),
            shipment.Status.ToString(),
            shipment.TrackingNumber,
            shipment.LastSyncedAtUtc,
            shipment.CreatedAt,
            shipment.UpdatedAt,
            items.Select(x => new ShipmentItemDto(x.OrderItemId.Value, x.Quantity)).ToList(),
            events);

    public static ShipmentSummaryDto ToSummary(Shipment shipment, IReadOnlyList<ShipmentItem> items) =>
        new(
            shipment.Id.Value,
            shipment.OrderId.Value,
            shipment.ShipmentNumber,
            shipment.Status.ToString(),
            shipment.TrackingNumber,
            items.Sum(x => x.Quantity),
            shipment.CreatedAt);
}
