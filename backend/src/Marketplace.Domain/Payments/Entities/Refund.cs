using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Enums;

namespace Marketplace.Domain.Payments.Entities;

public sealed class Refund : AuditableSoftDeleteAggregateRoot<RefundId>
{
    private Refund() { }

    public PaymentId PaymentId { get; private set; } = null!;
    public OrderId OrderId { get; private set; } = null!;
    public Money Amount { get; private set; } = Money.Zero;
    public string Reason { get; private set; } = string.Empty;
    public RefundStatus Status { get; private set; }
    public Guid? ProcessedByUserId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public static Refund Create(
        RefundId id,
        PaymentId paymentId,
        OrderId orderId,
        Money amount,
        string reason,
        Guid processedByUserId)
    {
        var now = DateTime.UtcNow;
        return new Refund
        {
            Id = id,
            PaymentId = paymentId,
            OrderId = orderId,
            Amount = amount,
            Reason = reason.Trim(),
            Status = RefundStatus.Pending,
            ProcessedByUserId = processedByUserId,
            ProcessedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public static Refund Reconstitute(
        RefundId id,
        PaymentId paymentId,
        OrderId orderId,
        Money amount,
        string reason,
        RefundStatus status,
        Guid? processedByUserId,
        DateTime? processedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            PaymentId = paymentId,
            OrderId = orderId,
            Amount = amount,
            Reason = reason,
            Status = status,
            ProcessedByUserId = processedByUserId,
            ProcessedAt = processedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void SetStatus(RefundStatus status)
    {
        Status = status;
        if (status is RefundStatus.Completed or RefundStatus.Rejected)
            ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
