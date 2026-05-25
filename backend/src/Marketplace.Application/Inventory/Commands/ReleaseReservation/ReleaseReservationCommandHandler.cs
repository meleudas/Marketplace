using System.Linq;
using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Exceptions;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReleaseReservation;

public sealed class ReleaseReservationCommandHandler : IRequestHandler<ReleaseReservationCommand, Result>
{
    private readonly IInventoryAccessService _access;
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IOutboxWriter _outbox;
    private readonly IAppTransactionPort _tx;
    private readonly IRestockAvailabilityNotifier _restockNotifier;

    public ReleaseReservationCommandHandler(
        IInventoryAccessService access,
        IInventoryReservationRepository reservationRepository,
        IWarehouseStockRepository stockRepository,
        IProductRepository productRepository,
        IAppCachePort cache,
        IOutboxWriter outbox,
        IAppTransactionPort tx,
        IRestockAvailabilityNotifier restockNotifier)
    {
        _access = access;
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _productRepository = productRepository;
        _cache = cache;
        _outbox = outbox;
        _tx = tx;
        _restockNotifier = restockNotifier;
    }

    public async Task<Result> Handle(ReleaseReservationCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result.Failure("Forbidden");

            var companyId = CompanyId.From(request.CompanyId);
            var reservation = await _reservationRepository.GetByCodeAsync(companyId, request.ReservationCode, ct);
            if (reservation is null)
                return Result.Failure("Reservation not found");
            if (reservation.Status != InventoryReservationStatus.Active)
                return Result.Success();

            var beforeRows = await _stockRepository.ListByProductAsync(companyId, reservation.ProductId, ct);
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
                        reservation.Release();
                        await _stockRepository.UpdateAsync(stock, innerCt);
                        await _reservationRepository.UpdateAsync(reservation, innerCt);
                    }, ct);
                    break;
                }
                catch (ConcurrencyConflictException) when (attempt < 2)
                {
                    await Task.Delay(25 * (attempt + 1), ct);
                }
            }
            await _outbox.AppendAsync(
                "InventoryReservation",
                reservation.Id.Value.ToString(),
                "InventoryReleased",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    messageId = Guid.NewGuid(),
                    reservationId = reservation.Id.Value,
                    companyId = request.CompanyId,
                    reservationCode = request.ReservationCode,
                    quantity = reservation.Quantity
                }),
                ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            var product = await _productRepository.GetByIdAsync(reservation.ProductId, ct);
            if (product is not null)
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

            var afterRows = await _stockRepository.ListByProductAsync(companyId, reservation.ProductId, ct);
            var afterAvailableSum = afterRows.Sum(x => x.Available);
            await _restockNotifier.NotifyIfCrossedFromZeroAsync(
                request.CompanyId,
                reservation.ProductId.Value,
                beforeAvailableSum,
                afterAvailableSum,
                ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to release reservation: {ex.Message}");
        }
    }
}
