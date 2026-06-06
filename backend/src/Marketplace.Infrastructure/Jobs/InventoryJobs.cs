using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Application.Common.Observability;
using Hangfire;

namespace Marketplace.Infrastructure.Jobs;

public sealed class InventoryJobs
{
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IWarehouseStockRepository _stockRepository;

    public InventoryJobs(
        IInventoryReservationRepository reservationRepository,
        IWarehouseStockRepository stockRepository)
    {
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task ExpireReservationsAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("inventory-expire-reservations", ExpireReservationsCoreAsync, ct);

    private async Task ExpireReservationsCoreAsync(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.HangfireJobLatencyMs,
            new KeyValuePair<string, object?>("job", "inventory-expire-reservations"));

        try
        {
            var expired = await _reservationRepository.ListExpiredActiveAsync(DateTime.UtcNow, ct);
            foreach (var reservation in expired)
            {
                var stock = await _stockRepository.GetByWarehouseAndProductAsync(reservation.WarehouseId, reservation.ProductId, ct);
                if (stock is not null)
                {
                    stock.Release(reservation.Quantity);
                    await _stockRepository.UpdateAsync(stock, ct);
                }

                reservation.Expire();
                await _reservationRepository.UpdateAsync(reservation, ct);
            }

            MarketplaceMetrics.HangfireJobs.Add(1, [
                new KeyValuePair<string, object?>("job", "inventory-expire-reservations"),
                new KeyValuePair<string, object?>("status", "success")
            ]);
        }
        catch
        {
            MarketplaceMetrics.HangfireJobErrors.Add(1, [
                new KeyValuePair<string, object?>("job", "inventory-expire-reservations")
            ]);
            MarketplaceMetrics.HangfireJobs.Add(1, [
                new KeyValuePair<string, object?>("job", "inventory-expire-reservations"),
                new KeyValuePair<string, object?>("status", "failed")
            ]);
            throw;
        }
    }
}
