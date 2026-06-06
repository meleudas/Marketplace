using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Favorites.Entities;
using Marketplace.Domain.Favorites.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class FavoriteRepository : IFavoriteRepository
{
    private readonly ApplicationDbContext _context;

    public FavoriteRepository(ApplicationDbContext context) => _context = context;

    public async Task<Favorite?> GetByUserAndProductAsync(Guid userId, ProductId productId, CancellationToken ct = default)
    {
        var row = await _context.Favorites
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Favorite?> GetByUserAndProductIncludingDeletedAsync(Guid userId, ProductId productId, CancellationToken ct = default)
    {
        var row = await _context.Favorites
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Favorite>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.Favorites
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AddedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Favorite> AddAsync(Favorite favorite, CancellationToken ct = default)
    {
        var row = ToRecord(favorite);
        await _context.Favorites.AddAsync(row, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.Message.Contains("IX_favorites_UserId_ProductId", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Active favorite already exists.", ex);
        }
        return ToDomain(row);
    }

    public async Task ReactivateAsync(FavoriteId id, DateTime utcNow, Money? priceAtAdd, CancellationToken ct = default)
    {
        var row = await _context.Favorites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (row is null)
            return;

        row.AddedAt = utcNow;
        row.PriceAtAdd = priceAtAdd?.Amount;
        row.IsAvailable = true;
        row.IsDeleted = false;
        row.DeletedAt = null;
        row.UpdatedAt = utcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(FavoriteId id, DateTime utcNow, CancellationToken ct = default)
    {
        var row = await _context.Favorites.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (row is null || row.IsDeleted)
            return;
        row.IsDeleted = true;
        row.DeletedAt = utcNow;
        row.UpdatedAt = utcNow;
        await _context.SaveChangesAsync(ct);
    }

    private static Favorite ToDomain(FavoriteRecord row) =>
        Favorite.Reconstitute(
            FavoriteId.From(row.Id),
            row.UserId,
            ProductId.From(row.ProductId),
            row.AddedAt,
            row.PriceAtAdd.HasValue ? new Money(row.PriceAtAdd.Value) : null,
            row.IsAvailable,
            new JsonBlob(row.NotificationsRaw),
            string.IsNullOrWhiteSpace(row.MetaRaw) ? null : new JsonBlob(row.MetaRaw),
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static FavoriteRecord ToRecord(Favorite favorite) =>
        new()
        {
            Id = favorite.Id.Value,
            UserId = favorite.UserId,
            ProductId = favorite.ProductId.Value,
            AddedAt = favorite.AddedAt,
            PriceAtAdd = favorite.PriceAtAdd?.Amount,
            IsAvailable = favorite.IsAvailable,
            NotificationsRaw = favorite.Notifications.Raw ?? "{}",
            MetaRaw = favorite.Meta?.Raw,
            CreatedAt = favorite.CreatedAt,
            UpdatedAt = favorite.UpdatedAt,
            IsDeleted = favorite.IsDeleted,
            DeletedAt = favorite.DeletedAt
        };
}
