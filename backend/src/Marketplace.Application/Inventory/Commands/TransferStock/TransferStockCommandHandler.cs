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

namespace Marketplace.Application.Inventory.Commands.TransferStock;

public sealed class TransferStockCommandHandler : IRequestHandler<TransferStockCommand, Result>
{
    private readonly IInventoryAccessService _access;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;

    public TransferStockCommandHandler(IInventoryAccessService access, IWarehouseStockRepository stockRepository, IStockMovementRepository movementRepository, IProductRepository productRepository, IAppCachePort cache)
    {
        _access = access;
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<Result> Handle(TransferStockCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result.Failure("Forbidden");

            var companyId = CompanyId.From(request.CompanyId);
            if (await _movementRepository.ExistsByOperationIdAsync(companyId, request.OperationId, ct))
                return Result.Failure("Operation already processed");

            var productId = ProductId.From(request.ProductId);
            var from = await _stockRepository.GetByWarehouseAndProductAsync(WarehouseId.From(request.FromWarehouseId), productId, ct);
            if (from is null)
                return Result.Failure("Source stock not found");

            var toWarehouseId = WarehouseId.From(request.ToWarehouseId);
            var to = await _stockRepository.GetByWarehouseAndProductAsync(toWarehouseId, productId, ct)
                ?? WarehouseStock.Create(WarehouseStockId.From(0), companyId, toWarehouseId, productId, 0, 0, 0);

            if (to.Id.Value == 0)
                await _stockRepository.AddAsync(to, ct);

            from.Ship(request.Quantity);
            to.Receive(request.Quantity);
            await _stockRepository.UpdateAsync(from, ct);
            await _stockRepository.UpdateAsync(to, ct);

            await _movementRepository.AddAsync(StockMovement.Create(
                StockMovementId.From(0), companyId, WarehouseId.From(request.FromWarehouseId), productId,
                StockMovementType.TransferOut, request.Quantity, $"{request.OperationId}:out", request.ActorUserId), ct);

            await _movementRepository.AddAsync(StockMovement.Create(
                StockMovementId.From(0), companyId, WarehouseId.From(request.ToWarehouseId), productId,
                StockMovementType.TransferIn, request.Quantity, $"{request.OperationId}:in", request.ActorUserId), ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            var product = await _productRepository.GetByIdAsync(productId, ct);
            if (product is not null)
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to transfer stock: {ex.Message}");
        }
    }
}
