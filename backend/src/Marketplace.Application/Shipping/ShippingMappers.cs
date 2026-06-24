using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shipping.Entities;

namespace Marketplace.Application.Shipping;

internal static class ShippingMappers
{
    public static UserAddressDto ToDto(this UserAddress entity) =>
        new(
            entity.Id.Value,
            entity.Type.ToString(),
            entity.IsDefault,
            entity.FirstName,
            entity.LastName,
            entity.Phone,
            entity.Street,
            entity.City,
            entity.State,
            entity.PostalCode,
            entity.Country);

    public static ShippingMethodDto ToDto(this ShippingMethod entity) =>
        new(
            entity.Id.Value,
            entity.Name,
            entity.Code.ToString(),
            entity.Price.Amount,
            entity.FreeShippingThreshold?.Amount,
            entity.EstimatedDaysMin,
            entity.EstimatedDaysMax);

    public static ShipmentDto ToDto(this Shipment entity) =>
        new(
            entity.Id.Value,
            entity.OrderId.Value,
            entity.ShipmentNumber,
            entity.ShippingMethodId.Value,
            entity.CarrierCode.ToString(),
            entity.Status.ToString(),
            entity.TrackingNumber,
            entity.LastSyncedAtUtc,
            entity.CreatedAt,
            entity.UpdatedAt);
}
