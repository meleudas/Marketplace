namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class CartRecord
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public short Status { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
