using System.Text.Json;
using Hangfire;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Services;
using Marketplace.Domain.Inventory.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class InventoryJobs
{
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IInventoryReservationReleaseService _releaseService;
    private readonly IIntegrationRetryStore _integrationRetryStore;
    private readonly IntegrationRetryOptions _retryOptions;

    public InventoryJobs(
        IInventoryReservationRepository reservationRepository,
        IInventoryReservationReleaseService releaseService,
        IIntegrationRetryStore integrationRetryStore,
        IOptions<IntegrationRetryOptions> retryOptions)
    {
        _reservationRepository = reservationRepository;
        _releaseService = releaseService;
        _integrationRetryStore = integrationRetryStore;
        _retryOptions = retryOptions.Value;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task ExpireReservationsAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("inventory-expire-reservations", ExpireReservationsCoreAsync, ct);

    private async Task ExpireReservationsCoreAsync(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "inventory-expire-reservations"));

        var hadFailures = false;
        var expired = await _reservationRepository.ListExpiredActiveAsync(DateTime.UtcNow, ct);
        foreach (var reservation in expired)
        {
            try
            {
                await _releaseService.ReleaseAsync(reservation, null, "expired", expired: true, ct);
            }
            catch (Exception ex)
            {
                hadFailures = true;
                MarketplaceMetrics.HangfireJobErrors.Add(1, [
                    new KeyValuePair<string, object?>("job", "inventory-expire-reservations")
                ]);
                var nextAttempt = RetryBackoffCalculator.ComputeNextAttemptUtc(
                    1,
                    _retryOptions.BaseBackoffMinutes,
                    _retryOptions.MaxBackoffMinutes,
                    DateTime.UtcNow);
                await _integrationRetryStore.UpsertAsync(
                    new IntegrationRetryUpsert(
                        IntegrationRetryKinds.InventoryExpire,
                        "InventoryReservation",
                        reservation.Id.Value.ToString(),
                        JsonSerializer.Serialize(new { reservationId = reservation.Id.Value }),
                        ex.Message),
                    nextAttempt,
                    ct);
            }
        }

        MarketplaceMetrics.HangfireJobs.Add(1, [
            new KeyValuePair<string, object?>("job", "inventory-expire-reservations"),
            new KeyValuePair<string, object?>("status", hadFailures ? "partial" : "success")
        ]);
    }
}
