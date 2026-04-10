using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Enums;

namespace Marketplace.Domain.Inventory.Entities;

public sealed class InventoryReservation : AuditableSoftDeleteAggregateRoot<InventoryReservationId>
{
    private InventoryReservation() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public WarehouseId WarehouseId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public string ReservationCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? Reference { get; private set; }

    public static InventoryReservation Create(
        InventoryReservationId id,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        string reservationCode,
        int quantity,
        DateTime expiresAt,
        string? reference = null)
    {
        if (string.IsNullOrWhiteSpace(reservationCode))
            throw new DomainException("Reservation code is required");
        if (quantity <= 0)
            throw new DomainException("Reservation quantity must be positive");
        if (expiresAt <= DateTime.UtcNow)
            throw new DomainException("Reservation expiration must be in the future");

        var now = DateTime.UtcNow;
        return new InventoryReservation
        {
            Id = id,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            ReservationCode = reservationCode.Trim(),
            Quantity = quantity,
            Status = InventoryReservationStatus.Active,
            ExpiresAt = expiresAt,
            Reference = reference,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static InventoryReservation Reconstitute(
        InventoryReservationId id,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        string reservationCode,
        int quantity,
        InventoryReservationStatus status,
        DateTime expiresAt,
        string? reference,
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
            ReservationCode = reservationCode,
            Quantity = quantity,
            Status = status,
            ExpiresAt = expiresAt,
            Reference = reference,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void Confirm()
    {
        EnsureActive();
        Status = InventoryReservationStatus.Confirmed;
        Touch();
    }

    public void Release()
    {
        EnsureActive();
        Status = InventoryReservationStatus.Released;
        Touch();
    }

    public void Expire()
    {
        if (Status != InventoryReservationStatus.Active)
            return;

        Status = InventoryReservationStatus.Expired;
        Touch();
    }

    private void EnsureActive()
    {
        if (Status != InventoryReservationStatus.Active)
            throw new DomainException("Reservation is not active");
    }
}
