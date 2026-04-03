using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Support.Entities;

public sealed class SupportTicketMessage : AuditableSoftDeleteAggregateRoot<SupportTicketMessageId>
{
    private SupportTicketMessage() { }

    public SupportTicketId TicketId { get; private set; } = null!;
    public string SenderId { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public JsonBlob? Attachments { get; private set; }
    public bool IsInternal { get; private set; }

    public static SupportTicketMessage Reconstitute(
        SupportTicketMessageId id,
        SupportTicketId ticketId,
        string senderId,
        string message,
        JsonBlob? attachments,
        bool isInternal,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            TicketId = ticketId,
            SenderId = senderId,
            Message = message,
            Attachments = attachments,
            IsInternal = isInternal,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
