using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context) => _context = context;

    public async Task<Company?> GetByIdAsync(CompanyId id, CancellationToken ct = default)
    {
        var row = await _context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row == null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _context.Companies
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Company>> GetApprovedAsync(CancellationToken ct = default)
    {
        var rows = await _context.Companies
            .AsNoTracking()
            .Where(x => x.IsApproved)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Company>> GetPendingApprovalAsync(CancellationToken ct = default)
    {
        var rows = await _context.Companies
            .AsNoTracking()
            .Where(x => !x.IsApproved)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        await _context.Companies.AddAsync(ToRecord(company), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Company company, CancellationToken ct = default)
    {
        var row = await _context.Companies.FirstOrDefaultAsync(x => x.Id == company.Id.Value, ct)
            ?? throw new InvalidOperationException($"Company '{company.Id.Value}' was not found.");

        MapToRecord(company, row);
        await _context.SaveChangesAsync(ct);
    }

    private static Company ToDomain(CompanyRecord r)
    {
        var address = Address.Create(
            r.AddressStreet,
            r.AddressCity,
            r.AddressState,
            r.AddressPostalCode,
            r.AddressCountry);

        return Company.Reconstitute(
            CompanyId.From(r.Id),
            r.Name,
            r.Slug,
            r.Description,
            r.ImageUrl,
            r.ContactEmail,
            r.ContactPhone,
            address,
            r.IsApproved,
            r.ApprovedAt,
            r.ApprovedByUserId,
            r.Rating,
            0,
            0,
            new JsonBlob(r.MetaRaw),
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);
    }

    private static CompanyRecord ToRecord(Company company) =>
        new()
        {
            Id = company.Id.Value,
            Name = company.Name,
            Slug = company.Slug,
            Description = company.Description,
            ImageUrl = company.ImageUrl,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            AddressStreet = company.Address.Street,
            AddressCity = company.Address.City,
            AddressState = company.Address.State,
            AddressPostalCode = company.Address.PostalCode,
            AddressCountry = company.Address.Country,
            IsApproved = company.IsApproved,
            ApprovedAt = company.ApprovedAt,
            ApprovedByUserId = company.ApprovedByUserId,
            Rating = company.Rating,
            MetaRaw = company.Meta.Raw,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            IsDeleted = company.IsDeleted,
            DeletedAt = company.DeletedAt
        };

    private static void MapToRecord(Company company, CompanyRecord row)
    {
        row.Name = company.Name;
        row.Slug = company.Slug;
        row.Description = company.Description;
        row.ImageUrl = company.ImageUrl;
        row.ContactEmail = company.ContactEmail;
        row.ContactPhone = company.ContactPhone;
        row.AddressStreet = company.Address.Street;
        row.AddressCity = company.Address.City;
        row.AddressState = company.Address.State;
        row.AddressPostalCode = company.Address.PostalCode;
        row.AddressCountry = company.Address.Country;
        row.IsApproved = company.IsApproved;
        row.ApprovedAt = company.ApprovedAt;
        row.ApprovedByUserId = company.ApprovedByUserId;
        row.Rating = company.Rating;
        row.MetaRaw = company.Meta.Raw;
        row.UpdatedAt = company.UpdatedAt;
        row.IsDeleted = company.IsDeleted;
        row.DeletedAt = company.DeletedAt;
    }
}
