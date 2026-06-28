using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CompanyContractRepository : ICompanyContractRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyContractRepository(ApplicationDbContext context) => _context = context;

    public async Task<CompanyContract?> GetActiveByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var row = await _context.CompanyContracts.AsNoTracking().FirstOrDefaultAsync(
            x => x.CompanyId == companyId.Value && x.Status == (short)CompanyContractStatus.Active,
            ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CompanyContract?> GetByIdAsync(CompanyContractId id, CancellationToken ct = default)
    {
        var row = await _context.CompanyContracts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CompanyContract> AddAsync(CompanyContract contract, CancellationToken ct = default)
    {
        var row = ToRecord(contract);
        await _context.CompanyContracts.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    private static CompanyContract ToDomain(CompanyContractRecord row) =>
        CompanyContract.Reconstitute(
            CompanyContractId.From(row.Id),
            CompanyId.From(row.CompanyId),
            row.ContractNumber,
            (CompanyContractStatus)row.Status,
            row.EffectiveFrom,
            row.EffectiveTo,
            row.SignedAt,
            row.Notes,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static CompanyContractRecord ToRecord(CompanyContract x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            ContractNumber = x.ContractNumber,
            Status = (short)x.Status,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            SignedAt = x.SignedAt,
            Notes = x.Notes,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
