using Marketplace.Application.Carts.Ports;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CartStockWatchRepository : ICartStockWatchRepository
{
    private readonly ApplicationDbContext _context;

    public CartStockWatchRepository(ApplicationDbContext context) => _context = context;

    public async Task UpsertAsync(Guid userId, long productId, CancellationToken ct = default)
    {
        var row = await _context.CartStockWatches
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);
        if (row is not null)
            return;

        var now = DateTime.UtcNow;
        await _context.CartStockWatches.AddAsync(
            new CartStockWatchRecord
            {
                UserId = userId,
                ProductId = productId,
                CreatedAtUtc = now,
                LastNotifiedAtUtc = null
            },
            ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, long productId, CancellationToken ct = default)
    {
        await _context.CartStockWatches
            .Where(x => x.UserId == userId && x.ProductId == productId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _context.CartStockWatches
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> ListUserIdsEligibleForNotifyAsync(
        long productId,
        TimeSpan minIntervalSinceLastNotify,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        var threshold = utcNow - minIntervalSinceLastNotify;
        return await _context.CartStockWatches.AsNoTracking()
            .Where(x => x.ProductId == productId
                        && (x.LastNotifiedAtUtc == null || x.LastNotifiedAtUtc < threshold))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task TouchLastNotifiedAsync(Guid userId, long productId, DateTime utcNow, CancellationToken ct = default)
    {
        await _context.CartStockWatches
            .Where(x => x.UserId == userId && x.ProductId == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastNotifiedAtUtc, utcNow), ct);
    }
}
