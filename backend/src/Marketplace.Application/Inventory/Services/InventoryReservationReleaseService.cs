using System.Text.Json;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Exceptions;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Inventory.Services;

public sealed class InventoryReservationReleaseService : IInventoryReservationReleaseService
{
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IOutboxWriter _outbox;
    private readonly IAppTransactionPort _tx;
    private readonly IRestockAvailabilityNotifier _restockNotifier;

    public InventoryReservationReleaseService(
        IWarehouseStockRepository stockRepository,
        IInventoryReservationRepository reservationRepository,
        IStockMovementRepository movementRepository,
        IProductRepository productRepository,
        IAppCachePort cache,
        IOutboxWriter outbox,
        IAppTransactionPort tx,
        IRestockAvailabilityNotifier restockNotifier)
    {
        _stockRepository = stockRepository;
        _reservationRepository = reservationRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _cache = cache;
        _outbox = outbox;
        _tx = tx;
        _restockNotifier = restockNotifier;
    }

    public async Task ReleaseAsync(InventoryReservation reservation, Guid? actorUserId, string reason, bool expired, CancellationToken ct = default)
    {
        if (reservation.Status != InventoryReservationStatus.Active)
            return;

        var beforeRows = await _stockRepository.ListByProductAsync(reservation.CompanyId, reservation.ProductId, ct);
        var beforeAvailableSum = beforeRows.Sum(x => x.Available);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async innerCt =>
                {
                    var stock = await _stockRepository.GetByWarehouseAndProductAsync(reservation.WarehouseId, reservation.ProductId, innerCt);
                    if (stock is null)
                        throw new InvalidOperationException("Stock not found");

                    stock.Release(reservation.Quantity);
                    if (expired)
                        reservation.Expire();
                    else
                        reservation.Release();

                    await _stockRepository.UpdateAsync(stock, innerCt);
                    await _reservationRepository.UpdateAsync(reservation, innerCt);

                    await _movementRepository.AddAsync(
                        StockMovement.Create(
                            StockMovementId.From(0),
                            reservation.CompanyId,
                            reservation.WarehouseId,
                            reservation.ProductId,
                            StockMovementType.Release,
                            reservation.Quantity,
                            reason,
                            actorUserId ?? Guid.Empty,
                            reference: reservation.Reference),
                        innerCt);

                    await _outbox.AppendAsync(
                        "InventoryReservation",
                        reservation.Id.Value.ToString(),
                        "InventoryReleased",
                        JsonSerializer.Serialize(new
                        {
                            messageId = DomainEventIds.ForInventoryEvent(reservation.Id.Value, expired ? "expired" : "released"),
                            reservationId = reservation.Id.Value,
                            companyId = reservation.CompanyId.Value,
                            productId = reservation.ProductId.Value,
                            reservationCode = reservation.ReservationCode,
                            quantity = reservation.Quantity,
                            reason
                        }),
                        innerCt);
                }, ct);
                break;
            }
            catch (ConcurrencyConflictException) when (attempt < 2)
            {
                await Task.Delay(25 * (attempt + 1), ct);
            }
        }

        await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
        var product = await _productRepository.GetByIdAsync(reservation.ProductId, ct);
        if (product is not null)
            await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

        var afterRows = await _stockRepository.ListByProductAsync(reservation.CompanyId, reservation.ProductId, ct);
        var afterAvailableSum = afterRows.Sum(x => x.Available);
        await _restockNotifier.NotifyIfCrossedFromZeroAsync(
            reservation.CompanyId.Value,
            reservation.ProductId.Value,
            beforeAvailableSum,
            afterAvailableSum,
            ct);
    }
}
