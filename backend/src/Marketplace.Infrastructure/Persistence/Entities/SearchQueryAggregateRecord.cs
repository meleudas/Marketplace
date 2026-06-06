namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SearchQueryAggregateRecord
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
