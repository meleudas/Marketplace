using Marketplace.Application.Common.Observability;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using System.Text.Json;

namespace Marketplace.Application.Shipping.Services;

public sealed record CreateShipmentLineRequest(long OrderItemId, int Quantity);

public interface IShipmentFulfillmentService
{
    Task<Result<Shipment>> CreateShipmentAsync(
        Order order,
        WarehouseId? warehouseId,
        IReadOnlyList<CreateShipmentLineRequest> lines,
        string? trackingNumber,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<Result> ApplyCarrierEventAsync(
        ShippingCarrierCode carrier,
        string eventKey,
        string payloadHash,
        string rawPayload,
        CancellationToken ct = default);

    Task<ShipmentFulfillmentSummary> BuildSummaryAsync(OrderId orderId, CancellationToken ct = default);

    Task<FulfillmentReadinessDto> BuildReadinessDtoAsync(OrderId orderId, CancellationToken ct = default);
}

public sealed class ShipmentFulfillmentService : IShipmentFulfillmentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipmentItemRepository _shipmentItemRepository;
    private readonly IShippingEventRepository _shippingEventRepository;
    private readonly IOrderStatusHistoryWriter _historyWriter;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly IOrderFulfillmentAllocationRepository _allocationRepository;
    private readonly IFulfillmentInventoryService _fulfillmentInventory;
    private readonly IWarehouseRepository _warehouseRepository;

    public ShipmentFulfillmentService(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IShipmentRepository shipmentRepository,
        IShipmentItemRepository shipmentItemRepository,
        IShippingEventRepository shippingEventRepository,
        IOrderStatusHistoryWriter historyWriter,
        OrderMutationCoordinator orderMutationCoordinator,
        IOrderFulfillmentAllocationRepository allocationRepository,
        IFulfillmentInventoryService fulfillmentInventory,
        IWarehouseRepository warehouseRepository)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _shipmentRepository = shipmentRepository;
        _shipmentItemRepository = shipmentItemRepository;
        _shippingEventRepository = shippingEventRepository;
        _historyWriter = historyWriter;
        _orderMutationCoordinator = orderMutationCoordinator;
        _allocationRepository = allocationRepository;
        _fulfillmentInventory = fulfillmentInventory;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<Shipment>> CreateShipmentAsync(
        Order order,
        WarehouseId? warehouseId,
        IReadOnlyList<CreateShipmentLineRequest> lines,
        string? trackingNumber,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (lines.Count == 0)
            return Result<Shipment>.Failure("At least one shipment line is required");

        if (order.Status is not (OrderStatus.Paid or OrderStatus.Processing))
            return Result<Shipment>.Failure("Shipments can only be created for paid or processing orders");

        var allocations = await _allocationRepository.ListByOrderIdAsync(order.Id, ct);
        var warehouseScoped = allocations.Count > 0;
        if (warehouseScoped && warehouseId is null)
            return Result<Shipment>.Failure("warehouseId is required for allocated orders");

        var orderItems = await _orderItemRepository.ListByOrderIdAsync(order.Id, ct);
        var shippedByItem = await GetShippedQuantitiesAsync(order.Id, ct);
        var shippedByWarehouse = warehouseId is null
            ? new Dictionary<long, int>()
            : await GetShippedQuantitiesByWarehouseAsync(order.Id, warehouseId, ct);

        foreach (var line in lines)
        {
            var orderItem = orderItems.FirstOrDefault(x => x.Id.Value == line.OrderItemId);
            if (orderItem is null)
                return Result<Shipment>.Failure($"Order item {line.OrderItemId} not found");

            if (warehouseScoped && warehouseId is not null)
            {
                var allocatedQty = allocations
                    .Where(x => x.WarehouseId == warehouseId
                        && x.OrderItemId.Value == line.OrderItemId
                        && x.Status is OrderFulfillmentAllocationStatus.Reserved
                            or OrderFulfillmentAllocationStatus.Confirmed)
                    .Sum(x => x.Quantity);
                var alreadyShipped = shippedByWarehouse.GetValueOrDefault(line.OrderItemId, 0);
                var remaining = allocatedQty - alreadyShipped;
                if (line.Quantity <= 0 || line.Quantity > remaining)
                    return Result<Shipment>.Failure($"Invalid quantity for order item {line.OrderItemId} at warehouse {warehouseId.Value}");
            }
            else
            {
                var alreadyShipped = shippedByItem.GetValueOrDefault(line.OrderItemId, 0);
                var remaining = orderItem.Quantity - alreadyShipped;
                if (line.Quantity <= 0 || line.Quantity > remaining)
                    return Result<Shipment>.Failure($"Invalid quantity for order item {line.OrderItemId}");
            }
        }

        var shipmentNumber = await _shipmentRepository.CountByOrderIdAsync(order.Id, ct) + 1;
        var shipment = Shipment.Create(
            ShipmentId.From(0),
            order.Id,
            order.CustomerId,
            shipmentNumber,
            order.ShippingMethodId,
            ShippingCarrierCode.Courier,
            warehouseId,
            trackingNumber);

        if (!string.IsNullOrWhiteSpace(trackingNumber))
            shipment.AssignTracking(trackingNumber);

        var savedShipment = await _shipmentRepository.AddAsync(shipment, ct);
        var shipmentItems = lines.Select(line =>
            ShipmentItem.Create(
                ShipmentItemId.From(0),
                savedShipment.Id,
                OrderItemId.From(line.OrderItemId),
                line.Quantity)).ToList();
        await _shipmentItemRepository.AddRangeAsync(shipmentItems, ct);

        if (warehouseScoped && warehouseId is not null)
        {
            var shipLines = lines
                .Select(line =>
                {
                    var orderItem = orderItems.First(x => x.Id.Value == line.OrderItemId);
                    return (orderItem.Id, orderItem.ProductId, line.Quantity);
                })
                .ToList();
            await _fulfillmentInventory.ShipAllocationsAsync(
                order.Id,
                order.CompanyId,
                warehouseId,
                shipLines,
                actorUserId,
                ct);
        }

        await RecalculateOrderFulfillmentAsync(order, actorUserId, ct);
        MarketplaceMetrics.ShipmentCreated.Add(1);
        return Result<Shipment>.Success(savedShipment);
    }

    public async Task<Result> ApplyCarrierEventAsync(
        ShippingCarrierCode carrier,
        string eventKey,
        string payloadHash,
        string rawPayload,
        CancellationToken ct = default)
    {
        if (await _shippingEventRepository.ExistsByDedupAsync(carrier, eventKey, payloadHash, ct))
            return Result.Success();

        var (trackingNumber, deliveryStatus) = ParseCarrierPayload(rawPayload);
        Shipment? shipment = null;
        if (!string.IsNullOrWhiteSpace(trackingNumber))
            shipment = await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber!, ct);

        var evt = ShippingEvent.CreateFromWebhook(
            ShippingEventId.From(0),
            carrier,
            eventKey,
            payloadHash,
            new JsonBlob(rawPayload),
            shipment?.Id,
            shipment?.OrderId,
            trackingNumber,
            deliveryStatus,
            DateTime.UtcNow);

        await _shippingEventRepository.AddAsync(evt, ct);
        MarketplaceMetrics.ShippingWebhookEvents.Add(1);

        if (shipment is null || deliveryStatus is null)
            return Result.Success();

        shipment.UpdateDeliveryStatus(deliveryStatus.Value);
        MarketplaceMetrics.ShipmentDeliveryStatus.Add(1, new KeyValuePair<string, object?>("status", deliveryStatus.Value.ToString()));
        shipment.MarkSynced(new JsonBlob(rawPayload));
        await _shipmentRepository.UpdateAsync(shipment, ct);

        var order = await _orderRepository.GetByIdAsync(shipment.OrderId, ct);
        if (order is not null)
            await RecalculateOrderFulfillmentAsync(order, Guid.Empty, ct);

        return Result.Success();
    }

    public async Task<FulfillmentReadinessDto> BuildReadinessDtoAsync(OrderId orderId, CancellationToken ct = default)
    {
        var orderItems = await _orderItemRepository.ListByOrderIdAsync(orderId, ct);
        var shippedByItem = await GetShippedQuantitiesAsync(orderId, ct);
        var summary = await BuildSummaryAsync(orderId, ct);
        var shipments = await _shipmentRepository.ListByOrderIdAsync(orderId, ct);

        var pending = orderItems
            .Select(oi =>
            {
                var shipped = shippedByItem.GetValueOrDefault(oi.Id.Value, 0);
                return new PendingShipmentItemDto(oi.Id.Value, oi.Quantity, shipped, oi.Quantity - shipped);
            })
            .Where(x => x.RemainingQuantity > 0)
            .ToList();

        var pendingByWarehouse = await BuildPendingByWarehouseAsync(orderId, orderItems, ct);

        var shipmentSummaries = new List<ShipmentSummaryDto>();
        foreach (var shipment in shipments)
        {
            var items = await _shipmentItemRepository.ListByShipmentIdAsync(shipment.Id, ct);
            shipmentSummaries.Add(ShipmentMapper.ToSummary(shipment, items));
        }

        return new FulfillmentReadinessDto(
            summary.TotalOrderItems,
            summary.FullyShippedItems,
            summary.FullyDeliveredItems,
            summary.IsFullyShipped,
            summary.IsFullyDelivered,
            pending,
            pendingByWarehouse,
            shipmentSummaries);
    }

    public async Task<ShipmentFulfillmentSummary> BuildSummaryAsync(OrderId orderId, CancellationToken ct = default)
    {
        var orderItems = await _orderItemRepository.ListByOrderIdAsync(orderId, ct);
        var shippedByItem = await GetShippedQuantitiesAsync(orderId, ct);
        var shipments = await _shipmentRepository.ListByOrderIdAsync(orderId, ct);

        var fullyShipped = orderItems.Count(x => shippedByItem.GetValueOrDefault(x.Id.Value, 0) >= x.Quantity);
        var isFullyShipped = orderItems.Count > 0 && fullyShipped == orderItems.Count;
        var isFullyDelivered = isFullyShipped && shipments.Count > 0 && shipments.All(x => x.Status == DeliveryStatus.Delivered);

        return new ShipmentFulfillmentSummary(
            orderItems.Count,
            fullyShipped,
            isFullyDelivered ? orderItems.Count : 0,
            isFullyShipped,
            isFullyDelivered);
    }

    private async Task RecalculateOrderFulfillmentAsync(Order order, Guid actorUserId, CancellationToken ct)
    {
        var summary = await BuildSummaryAsync(order.Id, ct);
        var shipments = await _shipmentRepository.ListByOrderIdAsync(order.Id, ct);
        var oldStatus = order.Status;

        if (summary.IsFullyDelivered && order.Status == OrderStatus.Shipped)
            order.SetDelivered();
        else if (summary.IsFullyShipped && order.Status is OrderStatus.Paid or OrderStatus.Processing)
        {
            var tracking = shipments.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.TrackingNumber))?.TrackingNumber;
            order.SetShipped(tracking);
        }

        if (oldStatus == order.Status)
            return;

        await _orderRepository.UpdateAsync(order, ct);
        await _historyWriter.WriteIfChangedAsync(order, oldStatus, actorUserId, "shipment", ct: ct);
        await _orderMutationCoordinator.PublishOrderChangedAsync(order, "OrderStatusChanged", $"shipment:{actorUserId}", ct);
    }

    private async Task<Dictionary<long, int>> GetShippedQuantitiesByWarehouseAsync(
        OrderId orderId,
        WarehouseId warehouseId,
        CancellationToken ct)
    {
        var shipments = await _shipmentRepository.ListByOrderIdAsync(orderId, ct);
        var shipmentIds = shipments
            .Where(x => x.WarehouseId == warehouseId)
            .Select(x => x.Id)
            .ToHashSet();
        if (shipmentIds.Count == 0)
            return new Dictionary<long, int>();

        var items = await _shipmentItemRepository.ListByOrderIdAsync(orderId, ct);
        return items
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .GroupBy(x => x.OrderItemId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
    }

    private async Task<IReadOnlyList<WarehouseFulfillmentGroupDto>> BuildPendingByWarehouseAsync(
        OrderId orderId,
        IReadOnlyList<OrderItem> orderItems,
        CancellationToken ct)
    {
        var allocations = await _allocationRepository.ListByOrderIdAsync(orderId, ct);
        if (allocations.Count == 0)
            return [];

        var groups = new List<WarehouseFulfillmentGroupDto>();
        foreach (var warehouseGroup in allocations
            .Where(x => x.Status is OrderFulfillmentAllocationStatus.Reserved or OrderFulfillmentAllocationStatus.Confirmed)
            .GroupBy(x => x.WarehouseId))
        {
            var warehouseId = warehouseGroup.Key;
            var shipped = await GetShippedQuantitiesByWarehouseAsync(orderId, warehouseId, ct);
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, ct);
            var items = new List<PendingShipmentItemDto>();

            foreach (var itemGroup in warehouseGroup.GroupBy(x => x.OrderItemId.Value))
            {
                var orderItem = orderItems.FirstOrDefault(x => x.Id.Value == itemGroup.Key);
                if (orderItem is null)
                    continue;

                var allocated = itemGroup.Sum(x => x.Quantity);
                var shippedQty = shipped.GetValueOrDefault(itemGroup.Key, 0);
                var remaining = allocated - shippedQty;
                if (remaining > 0)
                    items.Add(new PendingShipmentItemDto(itemGroup.Key, allocated, shippedQty, remaining));
            }

            if (items.Count > 0)
            {
                groups.Add(new WarehouseFulfillmentGroupDto(
                    warehouseId.Value,
                    warehouse?.Name ?? $"Warehouse {warehouseId.Value}",
                    items));
            }
        }

        return groups;
    }

    private async Task<Dictionary<long, int>> GetShippedQuantitiesAsync(OrderId orderId, CancellationToken ct)
    {
        var items = await _shipmentItemRepository.ListByOrderIdAsync(orderId, ct);
        return items
            .GroupBy(x => x.OrderItemId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
    }

    private static (string? TrackingNumber, DeliveryStatus? Status) ParseCarrierPayload(string rawPayload)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;
            var tracking = root.TryGetProperty("trackingNumber", out var t) ? t.GetString()
                : root.TryGetProperty("ttn", out var ttn) ? ttn.GetString() : null;
            DeliveryStatus? status = null;
            if (root.TryGetProperty("status", out var s))
            {
                status = s.GetString()?.ToLowerInvariant() switch
                {
                    "delivered" => DeliveryStatus.Delivered,
                    "in_transit" or "intransit" => DeliveryStatus.InTransit,
                    "failed" => DeliveryStatus.Failed,
                    "returned" => DeliveryStatus.Returned,
                    _ => DeliveryStatus.InTransit
                };
            }
            return (tracking, status);
        }
        catch
        {
            return (null, null);
        }
    }
}
