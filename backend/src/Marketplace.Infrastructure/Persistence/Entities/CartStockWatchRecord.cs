namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class CartStockWatchRecord
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public long ProductId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastNotifiedAtUtc { get; set; }
}
