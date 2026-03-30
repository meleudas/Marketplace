using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        var row = await _context.MarketplaceUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row == null ? null : ToDomain(row);
    }

    public async Task<User?> GetByIdentityIdAsync(IdentityUserId identityId, CancellationToken ct = default)
    {
        var row = await _context.MarketplaceUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == identityId.Value, ct);
        return row == null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _context.MarketplaceUsers
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<User>> SearchByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.Trim().ToLower();
        var rows = await _context.MarketplaceUsers
            .AsNoTracking()
            .Where(x => x.FirstName.ToLower().Contains(normalized) || x.LastName.ToLower().Contains(normalized))
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.MarketplaceUsers.AddAsync(ToRecord(user), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var row = await _context.MarketplaceUsers.FirstOrDefaultAsync(x => x.Id == user.Id.Value, ct)
            ?? throw new InvalidOperationException($"Marketplace user '{user.Id.Value}' was not found.");
        MapToRecord(user, row);
        await _context.SaveChangesAsync(ct);
    }

    private static User ToDomain(MarketplaceUserRecord r)
    {
        return User.Reconstitute(
            UserId.From(r.Id),
            r.FirstName,
            r.LastName,
            (UserRole)r.Role,
            r.Birthday,
            r.Avatar,
            r.IsVerified,
            r.VerificationDocument,
            r.LastLoginAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);
    }

    private static MarketplaceUserRecord ToRecord(User user) =>
        new()
        {
            Id = user.Id.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = (int)user.Role,
            Birthday = user.Birthday,
            Avatar = user.Avatar,
            IsVerified = user.IsVerified,
            VerificationDocument = user.VerificationDocument,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt
        };

    private static void MapToRecord(User user, MarketplaceUserRecord row)
    {
        row.FirstName = user.FirstName;
        row.LastName = user.LastName;
        row.Role = (int)user.Role;
        row.Birthday = user.Birthday;
        row.Avatar = user.Avatar;
        row.IsVerified = user.IsVerified;
        row.VerificationDocument = user.VerificationDocument;
        row.LastLoginAt = user.LastLoginAt;
        row.UpdatedAt = user.UpdatedAt;
        row.IsDeleted = user.IsDeleted;
        row.DeletedAt = user.DeletedAt;
    }
}
