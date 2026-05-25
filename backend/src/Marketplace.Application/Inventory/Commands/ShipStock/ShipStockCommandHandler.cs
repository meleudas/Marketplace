using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Exceptions;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Inventory.Mappings;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ShipStock;

public sealed class ShipStockCommandHandler : IRequestHandler<ShipStockCommand, Result<WarehouseStockDto>>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IAppTransactionPort _tx;

    public ShipStockCommandHandler(IInventoryAccessService access, IWarehouseStockRepository stockRepository, IStockMovementRepository movementRepository, IProductRepository productRepository, IAppCachePort cache, IAppTransactionPort tx)
    {
        _access = access;
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _cache = cache;
        _tx = tx;
    }

    public async Task<Result<WarehouseStockDto>> Handle(ShipStockCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result<WarehouseStockDto>.Failure("Forbidden");
            if (await _movementRepository.ExistsByOperationIdAsync(CompanyId.From(request.CompanyId), request.OperationId, ct))
                return Result<WarehouseStockDto>.Failure("Operation already processed");

            WarehouseStock? stock = null;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await _tx.ExecuteAsync(async innerCt =>
                    {
                        stock = await _stockRepository.GetByWarehouseAndProductAsync(WarehouseId.From(request.WarehouseId), ProductId.From(request.ProductId), innerCt);
                        if (stock is null)
                            throw new InvalidOperationException("Stock not found");

                        stock.Ship(request.Quantity);
                        await _stockRepository.UpdateAsync(stock, innerCt);

                        var movement = StockMovement.Create(
                            StockMovementId.From(0),
                            CompanyId.From(request.CompanyId),
                            WarehouseId.From(request.WarehouseId),
                            ProductId.From(request.ProductId),
                            StockMovementType.Outbound,
                            request.Quantity,
                            request.OperationId,
                            request.ActorUserId,
                            request.Reference);
                        await _movementRepository.AddAsync(movement, innerCt);
                    }, ct);
                    break;
                }
                catch (ConcurrencyConflictException) when (attempt < 2)
                {
                    await Task.Delay(25 * (attempt + 1), ct);
                }
            }

            if (stock is null)
                return Result<WarehouseStockDto>.Failure("Stock not found");
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is not null)
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);
            return Result<WarehouseStockDto>.Success(InventoryMapper.ToDto(stock));
        }
        catch (Exception ex)
        {
            return Result<WarehouseStockDto>.Failure($"Failed to ship stock: {ex.Message}");
        }
    }
}
