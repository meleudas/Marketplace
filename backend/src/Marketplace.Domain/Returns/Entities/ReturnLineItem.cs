using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Returns.Entities;

public sealed class ReturnLineItem : AuditableSoftDeleteAggregateRoot<ReturnLineItemId>
{
    private ReturnLineItem() { }

    public ReturnRequestId ReturnRequestId { get; private set; } = null!;
    public OrderItemId OrderItemId { get; private set; } = null!;
    public int Quantity { get; private set; }
    public string? Reason { get; private set; }

    public static ReturnLineItem Create(
        ReturnLineItemId id,
        ReturnRequestId returnRequestId,
        OrderItemId orderItemId,
        int quantity,
        string? reason = null)
    {
        if (quantity <= 0)
            throw new DomainException("Return line quantity must be positive");

        var now = DateTime.UtcNow;
        return new ReturnLineItem
        {
            Id = id,
            ReturnRequestId = returnRequestId,
            OrderItemId = orderItemId,
            Quantity = quantity,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static ReturnLineItem Reconstitute(
        ReturnLineItemId id,
        ReturnRequestId returnRequestId,
        OrderItemId orderItemId,
        int quantity,
        string? reason,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ReturnRequestId = returnRequestId,
            OrderItemId = orderItemId,
            Quantity = quantity,
            Reason = reason,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
