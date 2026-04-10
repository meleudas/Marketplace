using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Inventory.Entities;

public sealed class WarehouseStock : AuditableSoftDeleteAggregateRoot<WarehouseStockId>
{
    private WarehouseStock() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public WarehouseId WarehouseId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int ReorderPoint { get; private set; }
    public int Available => OnHand - Reserved;
    public long Version { get; private set; }

    public static WarehouseStock Create(
        WarehouseStockId id,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        int onHand,
        int reserved,
        int reorderPoint)
    {
        ValidateQuantities(onHand, reserved, reorderPoint);
        var now = DateTime.UtcNow;
        return new WarehouseStock
        {
            Id = id,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            OnHand = onHand,
            Reserved = reserved,
            ReorderPoint = reorderPoint,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static WarehouseStock Reconstitute(
        WarehouseStockId id,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        int onHand,
        int reserved,
        int reorderPoint,
        long version,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            OnHand = onHand,
            Reserved = reserved,
            ReorderPoint = reorderPoint,
            Version = version,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void Receive(int quantity)
    {
        EnsureNotDeleted();
        if (quantity <= 0)
            throw new DomainException("Receive quantity must be positive");

        OnHand += quantity;
        TouchVersioned();
    }

    public void Ship(int quantity)
    {
        EnsureNotDeleted();
        if (quantity <= 0)
            throw new DomainException("Ship quantity must be positive");
        if (quantity > Available)
            throw new DomainException("Cannot ship more than available stock");

        OnHand -= quantity;
        TouchVersioned();
    }

    public void Reserve(int quantity)
    {
        EnsureNotDeleted();
        if (quantity <= 0)
            throw new DomainException("Reserve quantity must be positive");
        if (quantity > Available)
            throw new DomainException("Cannot reserve more than available stock");

        Reserved += quantity;
        TouchVersioned();
    }

    public void Release(int quantity)
    {
        EnsureNotDeleted();
        if (quantity <= 0)
            throw new DomainException("Release quantity must be positive");
        if (quantity > Reserved)
            throw new DomainException("Cannot release more than reserved stock");

        Reserved -= quantity;
        TouchVersioned();
    }

    public void Adjust(int newOnHand, int newReserved, int reorderPoint)
    {
        EnsureNotDeleted();
        ValidateQuantities(newOnHand, newReserved, reorderPoint);
        OnHand = newOnHand;
        Reserved = newReserved;
        ReorderPoint = reorderPoint;
        TouchVersioned();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted warehouse stock");
    }

    private void TouchVersioned()
    {
        Version++;
        Touch();
    }

    private static void ValidateQuantities(int onHand, int reserved, int reorderPoint)
    {
        if (onHand < 0)
            throw new DomainException("OnHand cannot be negative");
        if (reserved < 0)
            throw new DomainException("Reserved cannot be negative");
        if (reorderPoint < 0)
            throw new DomainException("ReorderPoint cannot be negative");
        if (reserved > onHand)
            throw new DomainException("Reserved cannot be greater than onHand");
    }
}
