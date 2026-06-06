using Marketplace.Application.Shipping.Ports;
using Marketplace.Application.Common.Observability;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Shipping;

public sealed class NovaPoshtaClient : INovaPoshtaPort
{
    private readonly HttpClient _httpClient;
    private readonly NovaPoshtaOptions _options;

    public NovaPoshtaClient(HttpClient httpClient, IOptions<NovaPoshtaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<NovaPoshtaQuoteResult> CalculateQuoteAsync(NovaPoshtaQuoteRequest request, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.ShippingLatencyMs,
            new KeyValuePair<string, object?>("operation", "quote"));

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            MarketplaceMetrics.ShippingOps.Add(1, [new KeyValuePair<string, object?>("operation", "quote"), new KeyValuePair<string, object?>("status", "fallback")]);
            return new NovaPoshtaQuoteResult(
                true,
                _options.FallbackFlatRate,
                _options.FallbackEtaMinDays,
                _options.FallbackEtaMaxDays);
        }

        try
        {
            // Network request placeholder: for now keep stable fallback until full provider contract is finalized.
            _httpClient.BaseAddress = new Uri(_options.ApiUrl);
            MarketplaceMetrics.ShippingOps.Add(1, [new KeyValuePair<string, object?>("operation", "quote"), new KeyValuePair<string, object?>("status", "fallback")]);
            return new NovaPoshtaQuoteResult(
                true,
                _options.FallbackFlatRate,
                _options.FallbackEtaMinDays,
                _options.FallbackEtaMaxDays);
        }
        catch (Exception ex)
        {
            MarketplaceMetrics.ShippingErrors.Add(1, [new KeyValuePair<string, object?>("operation", "quote"), new KeyValuePair<string, object?>("reason", "exception")]);
            return new NovaPoshtaQuoteResult(false, 0m, 0, 0, ex.Message);
        }
    }

    public Task<NovaPoshtaStatusSyncResult> SyncStatusAsync(string trackingNumber, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.ShippingLatencyMs,
            new KeyValuePair<string, object?>("operation", "sync_status"));
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            MarketplaceMetrics.ShippingErrors.Add(1, [new KeyValuePair<string, object?>("operation", "sync_status"), new KeyValuePair<string, object?>("reason", "missing_tracking_number")]);
            return Task.FromResult(new NovaPoshtaStatusSyncResult(false, null, null, null, "Tracking number is required."));
        }

        MarketplaceMetrics.ShippingOps.Add(1, [new KeyValuePair<string, object?>("operation", "sync_status"), new KeyValuePair<string, object?>("status", "noop")]);
        return Task.FromResult(new NovaPoshtaStatusSyncResult(
            true,
            "InTransit",
            trackingNumber,
            "{}"));
    }
}
