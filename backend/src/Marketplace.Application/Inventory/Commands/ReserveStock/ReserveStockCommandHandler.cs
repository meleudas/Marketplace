using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReserveStock;

public sealed class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, Result>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;

    public ReserveStockCommandHandler(
        IInventoryAccessService access,
        IWarehouseStockRepository stockRepository,
        IInventoryReservationRepository reservationRepository,
        IStockMovementRepository movementRepository,
        IProductRepository productRepository,
        IAppCachePort cache)
    {
        _access = access;
        _stockRepository = stockRepository;
        _reservationRepository = reservationRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(ReserveStockCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result.Failure("Forbidden");

            var companyId = CompanyId.From(request.CompanyId);
            var existing = await _reservationRepository.GetByCodeAsync(companyId, request.ReservationCode, ct);
            if (existing is not null && existing.Status == InventoryReservationStatus.Active)
                return Result.Success();

            var stock = await _stockRepository.GetByWarehouseAndProductAsync(WarehouseId.From(request.WarehouseId), ProductId.From(request.ProductId), ct);
            if (stock is null)
                return Result.Failure("Stock not found");

            stock.Reserve(request.Quantity);
            await _stockRepository.UpdateAsync(stock, ct);

            var reservation = InventoryReservation.Create(
                InventoryReservationId.From(0),
                companyId,
                WarehouseId.From(request.WarehouseId),
                ProductId.From(request.ProductId),
                request.ReservationCode,
                request.Quantity,
                DateTime.UtcNow.AddMinutes(Math.Clamp(request.TtlMinutes, 1, 120)),
                request.Reference);
            await _reservationRepository.AddAsync(reservation, ct);

            await _movementRepository.AddAsync(
                StockMovement.Create(
                    StockMovementId.From(0),
                    companyId,
                    WarehouseId.From(request.WarehouseId),
                    ProductId.From(request.ProductId),
                    StockMovementType.Reserve,
                    request.Quantity,
                    $"reserve:{request.ReservationCode}",
                    request.ActorUserId,
                    reference: request.Reference),
                ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is not null)
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to reserve stock: {ex.Message}");
        }
    }
}
