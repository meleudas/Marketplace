using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Enums;

namespace Marketplace.Domain.Orders.Entities;

public sealed class OrderStatusHistory : AuditableSoftDeleteAggregateRoot<OrderStatusHistoryId>
{
    private OrderStatusHistory() { }

    public OrderId OrderId { get; private set; } = null!;
    public OrderStatus OldStatus { get; private set; }
    public OrderStatus NewStatus { get; private set; }
    public string? Comment { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public static OrderStatusHistory Create(
        OrderStatusHistoryId id,
        OrderId orderId,
        OrderStatus oldStatus,
        OrderStatus newStatus,
        Guid changedByUserId,
        string source,
        string? comment = null,
        string? correlationId = null)
    {
        var now = DateTime.UtcNow;
        return new OrderStatusHistory
        {
            Id = id,
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comment = comment,
            ChangedByUserId = changedByUserId,
            Source = source,
            CorrelationId = correlationId,
            ChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static OrderStatusHistory Reconstitute(
        OrderStatusHistoryId id,
        OrderId orderId,
        OrderStatus oldStatus,
        OrderStatus newStatus,
        string? comment,
        Guid changedByUserId,
        string source,
        string? correlationId,
        DateTime changedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comment = comment,
            ChangedByUserId = changedByUserId,
            Source = source,
            CorrelationId = correlationId,
            ChangedAt = changedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
