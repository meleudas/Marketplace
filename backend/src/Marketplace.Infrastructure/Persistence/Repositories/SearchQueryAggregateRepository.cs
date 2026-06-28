using Marketplace.Domain.Behavior.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SearchQueryAggregateRepository : ISearchQueryAggregateRepository
{
    private readonly ApplicationDbContext _context;

    public SearchQueryAggregateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<(string Query, long Count)>> GetTopQueriesAsync(DateOnly from, DateOnly to, int limit, CancellationToken ct = default)
    {
        var items = await _context.SearchQueryAggregates.AsNoTracking()
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderByDescending(x => x.Count)
            .Take(Math.Max(1, limit))
            .Select(x => new { x.Query, x.Count })
            .ToListAsync(ct);
        return items.Select(x => (x.Query, x.Count)).ToList();
    }
}
