using Marketplace.Domain.Inventory.Repositories;

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

    public async Task ExpireReservationsAsync(CancellationToken ct = default)
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
    }
}
