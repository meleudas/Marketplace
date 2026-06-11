using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Domain.Support.Entities;

public sealed class SupportTicket : AuditableSoftDeleteAggregateRoot<SupportTicketId>
{
    private SupportTicket() { }

    public string TicketNumber { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public OrderId? OrderId { get; private set; }
    public CompanyId? CompanyId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public SupportTicketStatus Status { get; private set; }
    public SupportTicketPriority Priority { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public string? AssignedToId { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime? EscalatedAt { get; private set; }
    public DateTime? SlaDueAt { get; private set; }

    public static SupportTicket Create(
        string ticketNumber,
        string userId,
        string subject,
        string message,
        SupportTicketPriority priority,
        OrderId? orderId,
        CompanyId? companyId,
        CategoryId? categoryId,
        DateTime slaDueAt,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        return new SupportTicket
        {
            Id = SupportTicketId.From(0),
            TicketNumber = ticketNumber,
            UserId = userId,
            OrderId = orderId,
            CompanyId = companyId,
            Subject = subject.Trim(),
            Message = message.Trim(),
            Status = SupportTicketStatus.Open,
            Priority = priority,
            CategoryId = categoryId,
            LastMessageAt = nowUtc,
            SlaDueAt = slaDueAt,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public bool CanTransitionTo(SupportTicketStatus next)
    {
        if (Status == SupportTicketStatus.Closed)
            return false;

        return (Status, next) switch
        {
            (SupportTicketStatus.Open, SupportTicketStatus.Assigned) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.PendingCustomer) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Escalated) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Assigned, SupportTicketStatus.PendingCustomer) => true,
            (SupportTicketStatus.Assigned, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.Assigned, SupportTicketStatus.Escalated) => true,
            (SupportTicketStatus.Assigned, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.PendingCustomer, SupportTicketStatus.Assigned) => true,
            (SupportTicketStatus.PendingCustomer, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.PendingCustomer, SupportTicketStatus.Escalated) => true,
            (SupportTicketStatus.PendingCustomer, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Escalated, SupportTicketStatus.Assigned) => true,
            (SupportTicketStatus.Escalated, SupportTicketStatus.PendingCustomer) => true,
            (SupportTicketStatus.Escalated, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.Escalated, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Open) => true,
            _ => false
        };
    }

    public Result Assign(string assigneeId, string actorId, string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(assigneeId))
            return Result.Failure("Assignee is required");
        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("conflict: ticket is closed");

        AssignedToId = assigneeId;
        if (Status == SupportTicketStatus.Open && CanTransitionTo(SupportTicketStatus.Assigned))
            Status = SupportTicketStatus.Assigned;
        UpdatedAt = nowUtc;
        _ = actorId;
        _ = reason;
        return Result.Success();
    }

    public Result UpdateStatus(SupportTicketStatus next, string actorId, string reason, DateTime nowUtc)
    {
        if (!CanTransitionTo(next))
            return Result.Failure("conflict: invalid ticket status transition");

        Status = next;
        if (next == SupportTicketStatus.Resolved)
            ResolvedAt = nowUtc;
        if (next == SupportTicketStatus.Closed)
            ClosedAt = nowUtc;
        if (next == SupportTicketStatus.Escalated)
            EscalatedAt = nowUtc;

        UpdatedAt = nowUtc;
        _ = actorId;
        _ = reason;
        return Result.Success();
    }

    public Result Escalate(string actorId, string reason, DateTime nowUtc)
    {
        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("conflict: ticket is closed");
        if (!CanTransitionTo(SupportTicketStatus.Escalated))
            return Result.Failure("conflict: cannot escalate ticket");

        Status = SupportTicketStatus.Escalated;
        EscalatedAt = nowUtc;
        UpdatedAt = nowUtc;
        _ = actorId;
        _ = reason;
        return Result.Success();
    }

    public void RecordCustomerMessage(string preview, DateTime nowUtc)
    {
        Message = preview.Length > 500 ? preview[..500] : preview;
        LastMessageAt = nowUtc;
        UpdatedAt = nowUtc;
        if (Status == SupportTicketStatus.PendingCustomer)
            return;
        if (CanTransitionTo(SupportTicketStatus.PendingCustomer))
            Status = SupportTicketStatus.PendingCustomer;
    }

    public bool IsSlaBreached(DateTime nowUtc) =>
        SlaDueAt.HasValue
        && nowUtc > SlaDueAt.Value
        && Status is not SupportTicketStatus.Resolved and not SupportTicketStatus.Closed;

    public static SupportTicket Reconstitute(
        SupportTicketId id,
        string ticketNumber,
        string userId,
        OrderId? orderId,
        CompanyId? companyId,
        string subject,
        string message,
        SupportTicketStatus status,
        SupportTicketPriority priority,
        CategoryId? categoryId,
        string? assignedToId,
        DateTime? lastMessageAt,
        DateTime? resolvedAt,
        DateTime? closedAt,
        DateTime? escalatedAt,
        DateTime? slaDueAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            TicketNumber = ticketNumber,
            UserId = userId,
            OrderId = orderId,
            CompanyId = companyId,
            Subject = subject,
            Message = message,
            Status = status,
            Priority = priority,
            CategoryId = categoryId,
            AssignedToId = assignedToId,
            LastMessageAt = lastMessageAt,
            ResolvedAt = resolvedAt,
            ClosedAt = closedAt,
            EscalatedAt = escalatedAt,
            SlaDueAt = slaDueAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
