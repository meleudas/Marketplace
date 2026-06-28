namespace Marketplace.Application.Shipping.Ports;

public interface INovaPoshtaPort
{
    Task<NovaPoshtaQuoteResult> CalculateQuoteAsync(NovaPoshtaQuoteRequest request, CancellationToken ct = default);
    Task<NovaPoshtaStatusSyncResult> SyncStatusAsync(string trackingNumber, CancellationToken ct = default);
}

public sealed record NovaPoshtaQuoteRequest(
    string City,
    string State,
    string PostalCode,
    string Country,
    decimal BaseAmount);

public sealed record NovaPoshtaQuoteResult(
    bool IsSuccess,
    decimal Amount,
    int EtaMinDays,
    int EtaMaxDays,
    string? Error = null);

public sealed record NovaPoshtaStatusSyncResult(
    bool IsSuccess,
    string? Status,
    string? TrackingNumber,
    string? RawPayload,
    string? Error = null);
