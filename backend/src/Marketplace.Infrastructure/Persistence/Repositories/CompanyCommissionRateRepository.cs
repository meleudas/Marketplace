using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CompanyCommissionRateRepository : ICompanyCommissionRateRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyCommissionRateRepository(ApplicationDbContext context) => _context = context;

    public async Task<CompanyCommissionRate?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var row = await _context.CompanyCommissionRates
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value && x.EffectiveFrom <= now && (x.EffectiveTo == null || x.EffectiveTo > now))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CompanyCommissionRate> AddAsync(CompanyCommissionRate rate, CancellationToken ct = default)
    {
        var row = ToRecord(rate);
        await _context.CompanyCommissionRates.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(CompanyCommissionRate rate, CancellationToken ct = default)
    {
        var row = await _context.CompanyCommissionRates.FirstOrDefaultAsync(x => x.Id == rate.Id.Value, ct)
            ?? throw new InvalidOperationException($"Commission rate '{rate.Id.Value}' was not found.");

        row.EffectiveTo = rate.EffectiveTo;
        row.UpdatedAt = rate.UpdatedAt;
        row.IsDeleted = rate.IsDeleted;
        row.DeletedAt = rate.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static CompanyCommissionRate ToDomain(CompanyCommissionRateRecord row) =>
        CompanyCommissionRate.Reconstitute(
            CompanyCommissionRateId.From(row.Id),
            CompanyId.From(row.CompanyId),
            CompanyContractId.From(row.ContractId),
            row.CommissionPercent,
            row.EffectiveFrom,
            row.EffectiveTo,
            row.Reason,
            row.CreatedByUserId,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CompanyCommissionRateRecord ToRecord(CompanyCommissionRate x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            ContractId = x.ContractId.Value,
            CommissionPercent = x.CommissionPercent,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            Reason = x.Reason,
            CreatedByUserId = x.CreatedByUserId,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
