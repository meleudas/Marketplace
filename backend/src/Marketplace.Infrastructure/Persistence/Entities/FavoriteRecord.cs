namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class FavoriteRecord
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public long ProductId { get; set; }
    public DateTime AddedAt { get; set; }
    public decimal? PriceAtAdd { get; set; }
    public bool IsAvailable { get; set; }
    public string NotificationsRaw { get; set; } = "{}";
    public string? MetaRaw { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
