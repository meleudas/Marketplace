using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Domain.Support.Entities;

public sealed class SupportTicket : AuditableSoftDeleteAggregateRoot<SupportTicketId>
{
    private SupportTicket() { }

    public string TicketNumber { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public OrderId? OrderId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public SupportTicketStatus Status { get; private set; }
    public SupportTicketPriority Priority { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public string? AssignedToId { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public static SupportTicket Reconstitute(
        SupportTicketId id,
        string ticketNumber,
        string userId,
        OrderId? orderId,
        string subject,
        string message,
        SupportTicketStatus status,
        SupportTicketPriority priority,
        CategoryId? categoryId,
        string? assignedToId,
        DateTime? lastMessageAt,
        DateTime? resolvedAt,
        DateTime? closedAt,
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
            Subject = subject,
            Message = message,
            Status = status,
            Priority = priority,
            CategoryId = categoryId,
            AssignedToId = assignedToId,
            LastMessageAt = lastMessageAt,
            ResolvedAt = resolvedAt,
            ClosedAt = closedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
