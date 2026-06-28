using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Enums;

namespace Marketplace.Domain.Inventory.Entities;

public sealed class OrderFulfillmentAllocation : AuditableSoftDeleteAggregateRoot<OrderFulfillmentAllocationId>
{
    private OrderFulfillmentAllocation() { }

    public OrderId OrderId { get; private set; } = null!;
    public OrderItemId OrderItemId { get; private set; } = null!;
    public CompanyId CompanyId { get; private set; } = null!;
    public WarehouseId WarehouseId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public int Quantity { get; private set; }
    public InventoryReservationId? ReservationId { get; private set; }
    public OrderFulfillmentAllocationStatus Status { get; private set; }

    public static OrderFulfillmentAllocation Create(
        OrderFulfillmentAllocationId id,
        OrderId orderId,
        OrderItemId orderItemId,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        int quantity,
        InventoryReservationId? reservationId = null)
    {
        if (quantity <= 0)
            throw new DomainException("Allocation quantity must be positive");

        var now = DateTime.UtcNow;
        return new OrderFulfillmentAllocation
        {
            Id = id,
            OrderId = orderId,
            OrderItemId = orderItemId,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            ReservationId = reservationId,
            Status = OrderFulfillmentAllocationStatus.Reserved,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static OrderFulfillmentAllocation Reconstitute(
        OrderFulfillmentAllocationId id,
        OrderId orderId,
        OrderItemId orderItemId,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        int quantity,
        InventoryReservationId? reservationId,
        OrderFulfillmentAllocationStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            OrderItemId = orderItemId,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            ReservationId = reservationId,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void AttachReservation(InventoryReservationId reservationId)
    {
        ReservationId = reservationId;
        Touch();
    }

    public void Confirm()
    {
        if (Status != OrderFulfillmentAllocationStatus.Reserved)
            throw new DomainException("Allocation is not reserved");
        Status = OrderFulfillmentAllocationStatus.Confirmed;
        Touch();
    }

    public void MarkShipped()
    {
        if (Status is not (OrderFulfillmentAllocationStatus.Reserved or OrderFulfillmentAllocationStatus.Confirmed))
            throw new DomainException("Allocation cannot be shipped");
        Status = OrderFulfillmentAllocationStatus.Shipped;
        Touch();
    }

    public void Release()
    {
        if (Status == OrderFulfillmentAllocationStatus.Shipped)
            throw new DomainException("Shipped allocation cannot be released");
        Status = OrderFulfillmentAllocationStatus.Released;
        Touch();
    }
}
