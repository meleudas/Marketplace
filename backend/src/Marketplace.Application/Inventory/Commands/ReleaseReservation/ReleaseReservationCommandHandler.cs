using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
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

    public ReleaseReservationCommandHandler(
        IInventoryAccessService access,
        IInventoryReservationRepository reservationRepository,
        IWarehouseStockRepository stockRepository,
        IProductRepository productRepository,
        IAppCachePort cache,
        IOutboxWriter outbox)
    {
        _access = access;
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _productRepository = productRepository;
        _cache = cache;
        _outbox = outbox;
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

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to release reservation: {ex.Message}");
        }
    }
}
