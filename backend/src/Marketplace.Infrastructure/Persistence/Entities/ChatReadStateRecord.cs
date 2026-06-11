namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ChatReadStateRecord
{
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public long LastReadMessageId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
