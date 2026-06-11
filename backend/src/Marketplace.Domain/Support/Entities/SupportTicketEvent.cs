using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Domain.Support.Entities;

public sealed class SupportTicketEvent : Entity
{
    private SupportTicketEvent() { }

    public long Id { get; private set; }
    public SupportTicketId TicketId { get; private set; } = null!;
    public SupportTicketEventType EventType { get; private set; }
    public string ActorUserId { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public JsonBlob Payload { get; private set; } = JsonBlob.Empty;
    public DateTime CreatedAt { get; private set; }

    public static SupportTicketEvent Create(
        SupportTicketId ticketId,
        SupportTicketEventType eventType,
        string actorUserId,
        string reason,
        JsonBlob payload,
        DateTime nowUtc) =>
        new()
        {
            TicketId = ticketId,
            EventType = eventType,
            ActorUserId = actorUserId,
            Reason = reason.Trim(),
            Payload = payload,
            CreatedAt = nowUtc
        };

    public static SupportTicketEvent Reconstitute(
        long id,
        SupportTicketId ticketId,
        SupportTicketEventType eventType,
        string actorUserId,
        string reason,
        JsonBlob payload,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            TicketId = ticketId,
            EventType = eventType,
            ActorUserId = actorUserId,
            Reason = reason,
            Payload = payload,
            CreatedAt = createdAt
        };
}
