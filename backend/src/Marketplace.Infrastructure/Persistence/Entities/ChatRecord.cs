namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ChatRecord
{
    public Guid Id { get; set; }
    public short Type { get; set; }
    public short Status { get; set; }
    public Guid InitiatorUserId { get; set; }
    public long? OrderId { get; set; }
    public long? ProductId { get; set; }
    public string? LastMessageText { get; set; }
    public Guid? LastMessageSenderId { get; set; }
    public DateTime? LastMessageCreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Meta { get; set; } = "{}";
    public string? ParticipantsSnapshot { get; set; }
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
