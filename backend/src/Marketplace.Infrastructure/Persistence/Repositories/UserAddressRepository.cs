using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class UserAddressRepository : IUserAddressRepository
{
    private readonly ApplicationDbContext _context;

    public UserAddressRepository(ApplicationDbContext context) => _context = context;

    public async Task<UserAddress?> GetByIdAsync(UserAddressId id, CancellationToken ct = default)
    {
        var row = await _context.UserAddresses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<UserAddress>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.UserAddresses.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<UserAddress> AddAsync(UserAddress entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.UserAddresses.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(UserAddress entity, CancellationToken ct = default)
    {
        var row = await _context.UserAddresses.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Address '{entity.Id.Value}' was not found.");

        row.Type = (short)entity.Type;
        row.IsDefault = entity.IsDefault;
        row.FirstName = entity.FirstName;
        row.LastName = entity.LastName;
        row.Phone = entity.Phone;
        row.Street = entity.Street;
        row.City = entity.City;
        row.State = entity.State;
        row.PostalCode = entity.PostalCode;
        row.Country = entity.Country;
        row.UpdatedAt = entity.UpdatedAt;
        row.IsDeleted = entity.IsDeleted;
        row.DeletedAt = entity.DeletedAt;

        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(UserAddressId id, DateTime deletedAtUtc, CancellationToken ct = default)
    {
        var row = await _context.UserAddresses.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (row is null)
            return;

        row.IsDeleted = true;
        row.DeletedAt = deletedAtUtc;
        row.UpdatedAt = deletedAtUtc;
        await _context.SaveChangesAsync(ct);
    }

    public async Task ClearDefaultAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.UserAddresses
            .Where(x => x.UserId == userId && x.IsDefault)
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.IsDefault = false;
            row.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static UserAddress ToDomain(UserAddressRecord row) =>
        UserAddress.Reconstitute(
            UserAddressId.From(row.Id),
            row.UserId,
            (UserAddressType)row.Type,
            row.IsDefault,
            ContactPerson.Create(row.FirstName, row.LastName, row.Phone),
            Address.Create(row.Street, row.City, row.State, row.PostalCode, row.Country),
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static UserAddressRecord ToRecord(UserAddress entity) =>
        new()
        {
            Id = entity.Id.Value,
            UserId = entity.UserId,
            Type = (short)entity.Type,
            IsDefault = entity.IsDefault,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Phone = entity.Phone,
            Street = entity.Street,
            City = entity.City,
            State = entity.State,
            PostalCode = entity.PostalCode,
            Country = entity.Country,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
