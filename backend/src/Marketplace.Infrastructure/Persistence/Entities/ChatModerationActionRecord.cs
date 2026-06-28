namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ChatModerationActionRecord
{
    public long Id { get; set; }
    public Guid ChatId { get; set; }
    public long? MessageId { get; set; }
    public short ActionType { get; set; }
    public Guid ModeratorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
