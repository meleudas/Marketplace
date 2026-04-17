using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CompanyMemberRepository : ICompanyMemberRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyMemberRepository(ApplicationDbContext context) => _context = context;

    public async Task<CompanyMember?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default)
    {
        var row = await _context.CompanyMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.UserId == userId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<CompanyMember>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.CompanyMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.IsOwner)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<CompanyMember>> ListByCompanyAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var rows = await _context.CompanyMembers
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .OrderByDescending(x => x.IsOwner)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public Task<bool> ExistsOwnerAsync(CompanyId companyId, CancellationToken ct = default)
        => _context.CompanyMembers.AnyAsync(x => x.CompanyId == companyId.Value && x.IsOwner, ct);

    public async Task AddAsync(CompanyMember member, CancellationToken ct = default)
    {
        await _context.CompanyMembers.AddAsync(ToRecord(member), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CompanyMember member, CancellationToken ct = default)
    {
        var row = await _context.CompanyMembers
            .FirstOrDefaultAsync(x => x.CompanyId == member.CompanyId.Value && x.UserId == member.UserId, ct)
            ?? throw new InvalidOperationException("Company member was not found.");

        MapToRecord(member, row);
        await _context.SaveChangesAsync(ct);
    }

    private static CompanyMember ToDomain(CompanyMemberRecord row) =>
        CompanyMember.Reconstitute(
            CompanyId.From(row.CompanyId),
            row.UserId,
            row.IsOwner,
            (CompanyMembershipRole)row.Role,
            new JsonBlob(row.PermissionsRaw),
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CompanyMemberRecord ToRecord(CompanyMember member) =>
        new()
        {
            CompanyId = member.CompanyId.Value,
            UserId = member.UserId,
            IsOwner = member.IsOwner,
            Role = (short)member.Role,
            PermissionsRaw = member.Permissions.Raw,
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt,
            IsDeleted = member.IsDeleted,
            DeletedAt = member.DeletedAt
        };

    private static void MapToRecord(CompanyMember member, CompanyMemberRecord row)
    {
        row.IsOwner = member.IsOwner;
        row.Role = (short)member.Role;
        row.PermissionsRaw = member.Permissions.Raw;
        row.UpdatedAt = member.UpdatedAt;
        row.IsDeleted = member.IsDeleted;
        row.DeletedAt = member.DeletedAt;
    }
}
