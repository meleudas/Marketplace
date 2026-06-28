using System.Linq;
using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Exceptions;
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
    private readonly IAppTransactionPort _tx;
    private readonly IRestockAvailabilityNotifier _restockNotifier;

    public TransferStockCommandHandler(
        IInventoryAccessService access,
        IWarehouseStockRepository stockRepository,
        IStockMovementRepository movementRepository,
        IProductRepository productRepository,
        IAppCachePort cache,
        IAppTransactionPort tx,
        IRestockAvailabilityNotifier restockNotifier)
    {
        _access = access;
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _cache = cache;
        _tx = tx;
        _restockNotifier = restockNotifier;
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
            var beforeRows = await _stockRepository.ListByProductAsync(companyId, productId, ct);
            var beforeAvailableSum = beforeRows.Sum(x => x.Available);

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await _tx.ExecuteAsync(async innerCt =>
                    {
                        var from = await _stockRepository.GetByWarehouseAndProductAsync(WarehouseId.From(request.FromWarehouseId), productId, innerCt);
                        if (from is null)
                            throw new InvalidOperationException("Source stock not found");

                        var toWarehouseId = WarehouseId.From(request.ToWarehouseId);
                        var to = await _stockRepository.GetByWarehouseAndProductAsync(toWarehouseId, productId, innerCt)
                                 ?? WarehouseStock.Create(WarehouseStockId.From(0), companyId, toWarehouseId, productId, 0, 0, 0);

                        if (to.Id.Value == 0)
                            await _stockRepository.AddAsync(to, innerCt);

                        from.Ship(request.Quantity);
                        to.Receive(request.Quantity);
                        await _stockRepository.UpdateAsync(from, innerCt);
                        await _stockRepository.UpdateAsync(to, innerCt);

                        await _movementRepository.AddAsync(StockMovement.Create(
                            StockMovementId.From(0), companyId, WarehouseId.From(request.FromWarehouseId), productId,
                            StockMovementType.TransferOut, request.Quantity, $"{request.OperationId}:out", request.ActorUserId), innerCt);

                        await _movementRepository.AddAsync(StockMovement.Create(
                            StockMovementId.From(0), companyId, WarehouseId.From(request.ToWarehouseId), productId,
                            StockMovementType.TransferIn, request.Quantity, $"{request.OperationId}:in", request.ActorUserId), innerCt);
                    }, ct);
                    break;
                }
                catch (ConcurrencyConflictException) when (attempt < 2)
                {
                    await Task.Delay(25 * (attempt + 1), ct);
                }
            }
            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            var product = await _productRepository.GetByIdAsync(productId, ct);
            if (product is not null)
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

            var afterRows = await _stockRepository.ListByProductAsync(companyId, productId, ct);
            var afterAvailableSum = afterRows.Sum(x => x.Available);
            await _restockNotifier.NotifyIfCrossedFromZeroAsync(
                request.CompanyId,
                request.ProductId,
                beforeAvailableSum,
                afterAvailableSum,
                ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to transfer stock: {ex.Message}");
        }
    }
}
