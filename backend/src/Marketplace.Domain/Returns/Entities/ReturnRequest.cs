using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Returns.Enums;

namespace Marketplace.Domain.Returns.Entities;

public sealed class ReturnRequest : AuditableSoftDeleteAggregateRoot<ReturnRequestId>
{
    private ReturnRequest() { }

    public OrderId OrderId { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public CompanyId CompanyId { get; private set; } = null!;
    public ReturnRequestStatus Status { get; private set; }
    public ReturnReasonCode ReasonCode { get; private set; }
    public string? Comment { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? RejectedReason { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public long? RefundId { get; private set; }

    public static ReturnRequest Create(
        ReturnRequestId id,
        OrderId orderId,
        Guid customerId,
        CompanyId companyId,
        ReturnReasonCode reasonCode,
        string? comment)
    {
        var now = DateTime.UtcNow;
        return new ReturnRequest
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            CompanyId = companyId,
            Status = ReturnRequestStatus.Requested,
            ReasonCode = reasonCode,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public void Approve(Guid actorUserId)
    {
        EnsureNotDeleted();
        if (Status != ReturnRequestStatus.Requested)
            throw new DomainException("Return can only be approved from Requested status");
        Status = ReturnRequestStatus.Approved;
        ApprovedByUserId = actorUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        EnsureNotDeleted();
        if (Status != ReturnRequestStatus.Requested)
            throw new DomainException("Return can only be rejected from Requested status");
        Status = ReturnRequestStatus.Rejected;
        RejectedReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReceived()
    {
        EnsureNotDeleted();
        if (Status != ReturnRequestStatus.Approved)
            throw new DomainException("Return can only be marked received from Approved status");
        Status = ReturnRequestStatus.Received;
        ReceivedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded(long refundId)
    {
        EnsureNotDeleted();
        if (Status != ReturnRequestStatus.Received)
            throw new DomainException("Return can only be refunded from Received status");
        Status = ReturnRequestStatus.Refunded;
        RefundId = refundId;
        UpdatedAt = DateTime.UtcNow;
    }

    public static ReturnRequest Reconstitute(
        ReturnRequestId id,
        OrderId orderId,
        Guid customerId,
        CompanyId companyId,
        ReturnRequestStatus status,
        ReturnReasonCode reasonCode,
        string? comment,
        Guid? approvedByUserId,
        string? rejectedReason,
        DateTime? receivedAtUtc,
        long? refundId,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            CompanyId = companyId,
            Status = status,
            ReasonCode = reasonCode,
            Comment = comment,
            ApprovedByUserId = approvedByUserId,
            RejectedReason = rejectedReason,
            ReceivedAtUtc = receivedAtUtc,
            RefundId = refundId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted return request");
    }
}
