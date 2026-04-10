using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Enums;

namespace Marketplace.Domain.Inventory.Entities;

public sealed class StockMovement : AuditableSoftDeleteAggregateRoot<StockMovementId>
{
    private StockMovement() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public WarehouseId WarehouseId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string OperationId { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public string? Reason { get; private set; }
    public Guid ActorUserId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public static StockMovement Create(
        StockMovementId id,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        StockMovementType type,
        int quantity,
        string operationId,
        Guid actorUserId,
        string? reference = null,
        string? reason = null)
    {
        if (quantity <= 0)
            throw new DomainException("Movement quantity must be positive");
        if (string.IsNullOrWhiteSpace(operationId))
            throw new DomainException("operationId is required for idempotency");

        var now = DateTime.UtcNow;
        return new StockMovement
        {
            Id = id,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            Type = type,
            Quantity = quantity,
            OperationId = operationId.Trim(),
            Reference = reference,
            Reason = reason,
            ActorUserId = actorUserId,
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static StockMovement Reconstitute(
        StockMovementId id,
        CompanyId companyId,
        WarehouseId warehouseId,
        ProductId productId,
        StockMovementType type,
        int quantity,
        string operationId,
        string? reference,
        string? reason,
        Guid actorUserId,
        DateTime occurredAt,
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
            Type = type,
            Quantity = quantity,
            OperationId = operationId,
            Reference = reference,
            Reason = reason,
            ActorUserId = actorUserId,
            OccurredAt = occurredAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
