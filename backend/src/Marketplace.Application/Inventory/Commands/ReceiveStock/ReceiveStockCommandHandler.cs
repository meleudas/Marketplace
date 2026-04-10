using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReceiveStock;

public sealed class ReceiveStockCommandHandler : IRequestHandler<ReceiveStockCommand, Result<WarehouseStockDto>>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IAppCachePort _cache;

    public ReceiveStockCommandHandler(
        IInventoryAccessService access,
        IWarehouseStockRepository stockRepository,
        IStockMovementRepository movementRepository,
        IAppCachePort cache)
    {
        _access = access;
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _cache = cache;
    }

    public async Task<Result<WarehouseStockDto>> Handle(ReceiveStockCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result<WarehouseStockDto>.Failure("Forbidden");

            if (await _movementRepository.ExistsByOperationIdAsync(CompanyId.From(request.CompanyId), request.OperationId, ct))
                return Result<WarehouseStockDto>.Failure("Operation already processed");

            var warehouseId = WarehouseId.From(request.WarehouseId);
            var productId = ProductId.From(request.ProductId);
            var stock = await _stockRepository.GetByWarehouseAndProductAsync(warehouseId, productId, ct)
                ?? WarehouseStock.Create(WarehouseStockId.From(0), CompanyId.From(request.CompanyId), warehouseId, productId, 0, 0, 0);

            if (stock.Id.Value == 0)
                await _stockRepository.AddAsync(stock, ct);

            stock.Receive(request.Quantity);
            await _stockRepository.UpdateAsync(stock, ct);

            var movement = StockMovement.Create(
                StockMovementId.From(0),
                CompanyId.From(request.CompanyId),
                warehouseId,
                productId,
                StockMovementType.Inbound,
                request.Quantity,
                request.OperationId,
                request.ActorUserId,
                request.Reference);
            await _movementRepository.AddAsync(movement, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);

            return Result<WarehouseStockDto>.Success(InventoryMapper.ToDto(stock));
        }
        catch (Exception ex)
        {
            return Result<WarehouseStockDto>.Failure($"Failed to receive stock: {ex.Message}");
        }
    }
}
