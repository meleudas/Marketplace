using System.Text.Json;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Inventory.Options;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Entities;
using Marketplace.Domain.Inventory.Enums;
using Marketplace.Domain.Inventory.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Inventory.Services;

public sealed class CheckoutInventoryService : ICheckoutInventoryService
{
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IOrderFulfillmentAllocationRepository _allocationRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryReservationReleaseService _releaseService;
    private readonly WarehouseAllocationPlanner _planner;
    private readonly IAppCachePort _cache;
    private readonly IOutboxWriter _outbox;
    private readonly CheckoutInventoryOptions _options;

    public CheckoutInventoryService(
        IWarehouseStockRepository stockRepository,
        IInventoryReservationRepository reservationRepository,
        IOrderFulfillmentAllocationRepository allocationRepository,
        IStockMovementRepository movementRepository,
        IProductRepository productRepository,
        IInventoryReservationReleaseService releaseService,
        WarehouseAllocationPlanner planner,
        IAppCachePort cache,
        IOutboxWriter outbox,
        IOptions<CheckoutInventoryOptions> options)
    {
        _stockRepository = stockRepository;
        _reservationRepository = reservationRepository;
        _allocationRepository = allocationRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _releaseService = releaseService;
        _planner = planner;
        _cache = cache;
        _outbox = outbox;
        _options = options.Value;
    }

    public async Task ReserveForOrderAsync(
        OrderId orderId,
        CompanyId companyId,
        IReadOnlyList<(OrderItemId OrderItemId, ProductId ProductId, int Quantity)> lines,
        CancellationToken ct = default)
    {
        var ttlMinutes = Math.Clamp(_options.ReservationTtlMinutes, 1, 120);
        var expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
        var orderReference = $"order:{orderId.Value}";

        var productToOrderItem = lines
            .GroupBy(x => x.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.First().OrderItemId);

        var plannerInput = lines
            .GroupBy(x => x.ProductId)
            .Select(g => new WarehouseAllocationLineRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var plan = await _planner.PlanAsync(companyId, plannerInput, ct);
        if (!plan.IsValid)
            throw new InvalidOperationException(plan.ErrorMessage ?? "Insufficient stock for checkout");

        var allocations = new List<OrderFulfillmentAllocation>();

        foreach (var planLine in plan.Lines)
        {
            var reservationCode = $"order-{orderId.Value}-product-{planLine.ProductId.Value}-wh-{planLine.WarehouseId.Value}";
            var existing = await _reservationRepository.GetByCodeAsync(companyId, reservationCode, ct);
            if (existing is not null && existing.Status == InventoryReservationStatus.Active)
                continue;

            var stock = await _stockRepository.GetByWarehouseAndProductAsync(planLine.WarehouseId, planLine.ProductId, ct)
                ?? throw new InvalidOperationException("Insufficient stock for checkout");

            stock.Reserve(planLine.Quantity);
            await _stockRepository.UpdateAsync(stock, ct);

            var reservation = InventoryReservation.Create(
                InventoryReservationId.From(0),
                companyId,
                planLine.WarehouseId,
                planLine.ProductId,
                reservationCode,
                planLine.Quantity,
                expiresAt,
                orderReference);
            await _reservationRepository.AddAsync(reservation, ct);

            var savedReservation = await _reservationRepository.GetByCodeAsync(companyId, reservationCode, ct)
                ?? throw new InvalidOperationException("Reservation was not persisted");

            var orderItemId = productToOrderItem[planLine.ProductId.Value];
            var allocation = OrderFulfillmentAllocation.Create(
                OrderFulfillmentAllocationId.From(0),
                orderId,
                orderItemId,
                companyId,
                planLine.WarehouseId,
                planLine.ProductId,
                planLine.Quantity,
                savedReservation.Id);
            allocations.Add(allocation);

            await _movementRepository.AddAsync(
                StockMovement.Create(
                    StockMovementId.From(0),
                    companyId,
                    planLine.WarehouseId,
                    planLine.ProductId,
                    StockMovementType.Reserve,
                    planLine.Quantity,
                    $"checkout:{reservationCode}",
                    Guid.Empty,
                    reference: orderReference),
                ct);

            await _outbox.AppendAsync(
                "InventoryReservation",
                savedReservation.Id.Value.ToString(),
                "InventoryReserved",
                JsonSerializer.Serialize(new
                {
                    messageId = DomainEventIds.ForInventoryEvent(savedReservation.Id.Value, "reserved"),
                    reservationId = savedReservation.Id.Value,
                    companyId = companyId.Value,
                    warehouseId = planLine.WarehouseId.Value,
                    productId = planLine.ProductId.Value,
                    quantity = planLine.Quantity,
                    reservationCode,
                    orderId = orderId.Value
                }),
                ct);

            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            var product = await _productRepository.GetByIdAsync(planLine.ProductId, ct);
            if (product is not null)
                await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);
        }

        if (allocations.Count > 0)
            await _allocationRepository.AddRangeAsync(allocations, ct);
    }

    public async Task ConfirmForOrderAsync(OrderId orderId, CompanyId companyId, CancellationToken ct = default)
    {
        var reservations = await _reservationRepository.ListActiveByReferenceAsync(companyId, $"order:{orderId.Value}", ct);
        foreach (var reservation in reservations)
        {
            reservation.Confirm();
            await _reservationRepository.UpdateAsync(reservation, ct);
        }

        var allocations = await _allocationRepository.ListByOrderIdAsync(orderId, ct);
        var toUpdate = new List<OrderFulfillmentAllocation>();
        foreach (var allocation in allocations)
        {
            if (allocation.Status != OrderFulfillmentAllocationStatus.Reserved)
                continue;
            allocation.Confirm();
            toUpdate.Add(allocation);
        }

        if (toUpdate.Count > 0)
            await _allocationRepository.UpdateRangeAsync(toUpdate, ct);
    }

    public async Task ReleaseForOrderAsync(OrderId orderId, CompanyId companyId, Guid? actorUserId, string reason, CancellationToken ct = default)
    {
        var reservations = await _reservationRepository.ListActiveByReferenceAsync(companyId, $"order:{orderId.Value}", ct);
        foreach (var reservation in reservations)
            await _releaseService.ReleaseAsync(reservation, actorUserId, reason, expired: false, ct);

        var allocations = await _allocationRepository.ListByOrderIdAsync(orderId, ct);
        var toUpdate = new List<OrderFulfillmentAllocation>();
        foreach (var allocation in allocations)
        {
            if (allocation.Status == OrderFulfillmentAllocationStatus.Shipped)
                continue;
            allocation.Release();
            toUpdate.Add(allocation);
        }

        if (toUpdate.Count > 0)
            await _allocationRepository.UpdateRangeAsync(toUpdate, ct);
    }
}
