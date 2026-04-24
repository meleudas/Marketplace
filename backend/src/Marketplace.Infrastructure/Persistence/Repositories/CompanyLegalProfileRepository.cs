using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CompanyLegalProfileRepository : ICompanyLegalProfileRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyLegalProfileRepository(ApplicationDbContext context) => _context = context;

    public async Task<CompanyLegalProfile?> GetByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var row = await _context.CompanyLegalProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task AddAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default)
    {
        await _context.CompanyLegalProfiles.AddAsync(ToRecord(legalProfile), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CompanyLegalProfile legalProfile, CancellationToken ct = default)
    {
        var row = await _context.CompanyLegalProfiles.FirstOrDefaultAsync(x => x.Id == legalProfile.Id.Value, ct)
            ?? throw new InvalidOperationException($"Company legal profile '{legalProfile.Id.Value}' was not found.");

        row.LegalName = legalProfile.LegalName;
        row.LegalType = (short)legalProfile.LegalType;
        row.Edrpou = legalProfile.Edrpou;
        row.Ipn = legalProfile.Ipn;
        row.CertificateNumber = legalProfile.CertificateNumber;
        row.IsVatPayer = legalProfile.IsVatPayer;
        row.InitialCommissionPercent = legalProfile.InitialCommissionPercent;
        row.UpdatedAt = legalProfile.UpdatedAt;
        row.IsDeleted = legalProfile.IsDeleted;
        row.DeletedAt = legalProfile.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static CompanyLegalProfile ToDomain(CompanyLegalProfileRecord row) =>
        CompanyLegalProfile.Reconstitute(
            CompanyLegalProfileId.From(row.Id),
            CompanyId.From(row.CompanyId),
            row.LegalName,
            (CompanyLegalType)row.LegalType,
            row.Edrpou,
            row.Ipn,
            row.CertificateNumber,
            row.IsVatPayer,
            row.InitialCommissionPercent,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CompanyLegalProfileRecord ToRecord(CompanyLegalProfile x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            LegalName = x.LegalName,
            LegalType = (short)x.LegalType,
            Edrpou = x.Edrpou,
            Ipn = x.Ipn,
            CertificateNumber = x.CertificateNumber,
            IsVatPayer = x.IsVatPayer,
            InitialCommissionPercent = x.InitialCommissionPercent,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
