using Marketplace.Application.Common;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Shipping.Ports;
using Marketplace.Application.Shipping.Services;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Infrastructure.Jobs;

public sealed class ShippingSyncJobs
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipmentFulfillmentService _fulfillment;
    private readonly INovaPoshtaPort _novaPoshtaPort;
    private readonly ILogger<ShippingSyncJobs> _logger;

    public ShippingSyncJobs(
        IShipmentRepository shipmentRepository,
        IShipmentFulfillmentService fulfillment,
        INovaPoshtaPort novaPoshtaPort,
        ILogger<ShippingSyncJobs> logger)
    {
        _shipmentRepository = shipmentRepository;
        _fulfillment = fulfillment;
        _novaPoshtaPort = novaPoshtaPort;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task SyncPendingAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("shipping-sync-pending", SyncPendingCoreAsync, ct);

    private async Task SyncPendingCoreAsync(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "shipping-sync-pending"));

        var inTransit = await _shipmentRepository.ListByStatusAsync(DeliveryStatus.InTransit, 50, ct);
        foreach (var shipment in inTransit)
        {
            if (string.IsNullOrWhiteSpace(shipment.TrackingNumber))
                continue;

            var sync = await _novaPoshtaPort.SyncStatusAsync(shipment.TrackingNumber, ct);
            if (!sync.IsSuccess)
            {
                _logger.LogWarning("Shipping sync failed for shipment {ShipmentId}", shipment.Id.Value);
                continue;
            }

            var payload = $"{{\"trackingNumber\":\"{shipment.TrackingNumber}\",\"status\":\"{sync.Status}\"}}";
            var eventKey = $"sync:{shipment.TrackingNumber}:{sync.Status}";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            await _fulfillment.ApplyCarrierEventAsync(
                ShippingCarrierCode.NovaPoshta,
                eventKey,
                hash,
                payload,
                ct);
        }

        MarketplaceMetrics.HangfireJobs.Add(1, new KeyValuePair<string, object?>("job", "shipping-sync-pending"));
    }
}
