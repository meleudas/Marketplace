using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context) => _context = context;

    public async Task<Cart?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await _context.Carts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == (short)CartStatus.Active, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Cart?> GetByIdAsync(CartId id, CancellationToken ct = default)
    {
        var row = await _context.Carts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Cart> AddAsync(Cart cart, CancellationToken ct = default)
    {
        var row = ToRecord(cart);
        await _context.Carts.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Cart cart, CancellationToken ct = default)
    {
        var row = await _context.Carts.FirstOrDefaultAsync(x => x.Id == cart.Id.Value, ct)
            ?? throw new InvalidOperationException("Cart not found");
        row.Status = (short)cart.Status;
        row.LastActivityAt = cart.LastActivityAt;
        row.UpdatedAt = cart.UpdatedAt;
        row.IsDeleted = cart.IsDeleted;
        row.DeletedAt = cart.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static Cart ToDomain(CartRecord row) =>
        Cart.Reconstitute(
            CartId.From(row.Id),
            row.UserId,
            (CartStatus)row.Status,
            row.LastActivityAt,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CartRecord ToRecord(Cart cart) =>
        new()
        {
            Id = cart.Id.Value,
            UserId = cart.UserId,
            Status = (short)cart.Status,
            LastActivityAt = cart.LastActivityAt,
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt,
            IsDeleted = cart.IsDeleted,
            DeletedAt = cart.DeletedAt
        };
}
