namespace Marketplace.Domain.Behavior.Repositories;

public interface ISearchQueryAggregateRepository
{
    Task<IReadOnlyList<(string Query, long Count)>> GetTopQueriesAsync(DateOnly from, DateOnly to, int limit, CancellationToken ct = default);
}
