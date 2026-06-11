namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ChatParticipantRecord
{
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public short Role { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
