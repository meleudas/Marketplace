using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
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
    private readonly IAppCachePort _cache;

    public ReleaseReservationCommandHandler(
        IInventoryAccessService access,
        IInventoryReservationRepository reservationRepository,
        IWarehouseStockRepository stockRepository,
        IAppCachePort cache)
    {
        _access = access;
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _cache = cache;
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

            var stock = await _stockRepository.GetByWarehouseAndProductAsync(reservation.WarehouseId, reservation.ProductId, ct);
            if (stock is null)
                return Result.Failure("Stock not found");

            stock.Release(reservation.Quantity);
            reservation.Release();
            await _stockRepository.UpdateAsync(stock, ct);
            await _reservationRepository.UpdateAsync(reservation, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to release reservation: {ex.Message}");
        }
    }
}
