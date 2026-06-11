namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ChatMessageRecord
{
    public long Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Attachments { get; set; } = "[]";
    public short Status { get; set; }
    public DateTime? ReadAt { get; set; }
    public string DeletedBy { get; set; } = "{}";
    public long? ReplyToMessageId { get; set; }
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
