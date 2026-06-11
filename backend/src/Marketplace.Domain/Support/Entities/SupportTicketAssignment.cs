using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Domain.Support.Entities;

public sealed class SupportTicketAssignment : Entity
{
    private SupportTicketAssignment() { }

    public long Id { get; private set; }
    public SupportTicketId TicketId { get; private set; } = null!;
    public string AssigneeUserId { get; private set; } = string.Empty;
    public string AssignedByUserId { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static SupportTicketAssignment Create(
        SupportTicketId ticketId,
        string assigneeUserId,
        string assignedByUserId,
        string reason,
        DateTime nowUtc) =>
        new()
        {
            TicketId = ticketId,
            AssigneeUserId = assigneeUserId,
            AssignedByUserId = assignedByUserId,
            Reason = reason.Trim(),
            CreatedAt = nowUtc
        };

    public static SupportTicketAssignment Reconstitute(
        long id,
        SupportTicketId ticketId,
        string assigneeUserId,
        string assignedByUserId,
        string reason,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            TicketId = ticketId,
            AssigneeUserId = assigneeUserId,
            AssignedByUserId = assignedByUserId,
            Reason = reason,
            CreatedAt = createdAt
        };
}
