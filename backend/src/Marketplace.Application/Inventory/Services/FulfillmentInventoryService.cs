using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;

namespace Marketplace.Application.Inventory.Services;

public interface IFulfillmentInventoryService
{
    Task ShipAllocationsAsync(
        OrderId orderId,
        CompanyId companyId,
        WarehouseId warehouseId,
        IReadOnlyList<(OrderItemId OrderItemId, ProductId ProductId, int Quantity)> lines,
        Guid actorUserId,
        CancellationToken ct = default);
}

public sealed class FulfillmentInventoryService : IFulfillmentInventoryService
{
    private readonly IOrderFulfillmentAllocationRepository _allocationRepository;
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IStockMovementRepository _movementRepository;

    public FulfillmentInventoryService(
        IOrderFulfillmentAllocationRepository allocationRepository,
        IWarehouseStockRepository stockRepository,
        IStockMovementRepository movementRepository)
    {
        _allocationRepository = allocationRepository;
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
    }

    public async Task ShipAllocationsAsync(
        OrderId orderId,
        CompanyId companyId,
        WarehouseId warehouseId,
        IReadOnlyList<(OrderItemId OrderItemId, ProductId ProductId, int Quantity)> lines,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var allocations = await _allocationRepository.ListByOrderAndWarehouseAsync(orderId, warehouseId, ct);
        if (allocations.Count == 0)
            return;

        foreach (var line in lines)
        {
            var remaining = line.Quantity;
            var matching = allocations
                .Where(x => x.OrderItemId == line.OrderItemId
                    && x.ProductId == line.ProductId
                    && x.Status is OrderFulfillmentAllocationStatus.Reserved or OrderFulfillmentAllocationStatus.Confirmed)
                .OrderBy(x => x.Id.Value)
                .ToList();

            foreach (var allocation in matching)
            {
                if (remaining <= 0)
                    break;

                if (allocation.Quantity > remaining)
                    throw new InvalidOperationException($"Partial allocation ship not supported for order item {line.OrderItemId.Value}");

                var shipQty = allocation.Quantity;
                var stock = await _stockRepository.GetByWarehouseAndProductAsync(warehouseId, line.ProductId, ct)
                    ?? throw new InvalidOperationException("Stock not found for shipment");

                stock.Ship(shipQty);
                await _stockRepository.UpdateAsync(stock, ct);

                await _movementRepository.AddAsync(
                    StockMovement.Create(
                        StockMovementId.From(0),
                        companyId,
                        warehouseId,
                        line.ProductId,
                        StockMovementType.Outbound,
                        shipQty,
                        $"shipment:order-{orderId.Value}:wh-{warehouseId.Value}:item-{line.OrderItemId.Value}",
                        actorUserId,
                        reference: $"order:{orderId.Value}"),
                    ct);

                allocation.MarkShipped();
                await _allocationRepository.UpdateAsync(allocation, ct);
                remaining -= shipQty;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"Insufficient allocated stock for order item {line.OrderItemId.Value}");
        }
    }
}
