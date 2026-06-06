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
    long ShippingMethodId,
    string CarrierCode,
    string Status,
    string? TrackingNumber,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedAt,
    DateTime UpdatedAt);
