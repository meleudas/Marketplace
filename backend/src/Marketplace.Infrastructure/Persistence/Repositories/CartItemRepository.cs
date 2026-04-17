using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CartItemRepository : ICartItemRepository
{
    private readonly ApplicationDbContext _context;

    public CartItemRepository(ApplicationDbContext context) => _context = context;

    public async Task<CartItem?> GetByIdAsync(CartItemId id, CancellationToken ct = default)
    {
        var row = await _context.CartItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CartItem?> GetByCartAndProductAsync(CartId cartId, ProductId productId, CancellationToken ct = default)
    {
        var row = await _context.CartItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CartId == cartId.Value && x.ProductId == productId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<CartItem>> ListByCartIdAsync(CartId cartId, CancellationToken ct = default)
    {
        var rows = await _context.CartItems
            .AsNoTracking()
            .Where(x => x.CartId == cartId.Value)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<CartItem> AddAsync(CartItem item, CancellationToken ct = default)
    {
        var row = ToRecord(item);
        await _context.CartItems.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(CartItem item, CancellationToken ct = default)
    {
        var row = await _context.CartItems.FirstOrDefaultAsync(x => x.Id == item.Id.Value, ct)
            ?? throw new InvalidOperationException("Cart item not found");
        row.Quantity = item.Quantity;
        row.PriceAtMoment = item.PriceAtMoment.Amount;
        row.Discount = item.Discount.Amount;
        row.UpdatedAt = item.UpdatedAt;
        row.IsDeleted = item.IsDeleted;
        row.DeletedAt = item.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(CartItemId id, DateTime utcNow, CancellationToken ct = default)
    {
        var row = await _context.CartItems.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (row is null || row.IsDeleted)
            return;
        row.IsDeleted = true;
        row.DeletedAt = utcNow;
        row.UpdatedAt = utcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteByCartIdAsync(CartId cartId, DateTime utcNow, CancellationToken ct = default)
    {
        var rows = await _context.CartItems
            .Where(x => x.CartId == cartId.Value && !x.IsDeleted)
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.DeletedAt = utcNow;
            row.UpdatedAt = utcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static CartItem ToDomain(CartItemRecord row) =>
        CartItem.Reconstitute(
            CartItemId.From(row.Id),
            CartId.From(row.CartId),
            ProductId.From(row.ProductId),
            row.Quantity,
            new Money(row.PriceAtMoment),
            new Money(row.Discount),
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CartItemRecord ToRecord(CartItem item) =>
        new()
        {
            Id = item.Id.Value,
            CartId = item.CartId.Value,
            ProductId = item.ProductId.Value,
            Quantity = item.Quantity,
            PriceAtMoment = item.PriceAtMoment.Amount,
            Discount = item.Discount.Amount,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            IsDeleted = item.IsDeleted,
            DeletedAt = item.DeletedAt
        };
}
