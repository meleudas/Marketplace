namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class BehaviorDailyAggregateRecord
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public short EventType { get; set; }
    public long Count { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
