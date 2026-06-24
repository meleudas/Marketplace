using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SettlementBatchRepository : ISettlementBatchRepository
{
    private readonly ApplicationDbContext _context;

    public SettlementBatchRepository(ApplicationDbContext context) => _context = context;

    public async Task<SettlementBatch?> GetByIdAsync(SettlementBatchId id, CancellationToken ct = default)
    {
        var row = await _context.SettlementBatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<SettlementBatch?> GetOpenByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var row = await _context.SettlementBatches
            .FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.Status == (short)SettlementBatchStatus.Open, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<SettlementBatch>> ListByStatusAsync(SettlementBatchStatus status, CancellationToken ct = default)
    {
        var rows = await _context.SettlementBatches.AsNoTracking()
            .Where(x => x.Status == (short)status)
            .OrderBy(x => x.PeriodEndUtc)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SettlementBatch>> ListByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var rows = await _context.SettlementBatches.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .OrderByDescending(x => x.PeriodEndUtc)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SettlementBatch>> ListAsync(
        SettlementBatchStatus? status,
        CompanyId? companyId,
        CancellationToken ct = default)
    {
        var query = _context.SettlementBatches.AsNoTracking().AsQueryable();
        if (status is not null)
            query = query.Where(x => x.Status == (short)status.Value);
        if (companyId is not null)
            query = query.Where(x => x.CompanyId == companyId.Value);

        var rows = await query.OrderByDescending(x => x.PeriodEndUtc).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<SettlementBatch> AddAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        var row = ToRecord(batch);
        await _context.SettlementBatches.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        var row = await _context.SettlementBatches.FirstOrDefaultAsync(x => x.Id == batch.Id.Value, ct)
            ?? throw new InvalidOperationException($"Settlement batch '{batch.Id.Value}' was not found.");

        row.Status = (short)batch.Status;
        row.TotalAmount = batch.TotalAmount;
        row.ClosedAtUtc = batch.ClosedAtUtc;
        row.PaidAtUtc = batch.PaidAtUtc;
        row.UpdatedAt = batch.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static SettlementBatch ToDomain(SettlementBatchRecord row) =>
        SettlementBatch.Reconstitute(
            SettlementBatchId.From(row.Id),
            CompanyId.From(row.CompanyId),
            row.PeriodStartUtc,
            row.PeriodEndUtc,
            (SettlementBatchStatus)row.Status,
            row.TotalAmount,
            row.Currency,
            row.ClosedAtUtc,
            row.PaidAtUtc,
            row.CreatedAt,
            row.UpdatedAt);

    private static SettlementBatchRecord ToRecord(SettlementBatch x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            PeriodStartUtc = x.PeriodStartUtc,
            PeriodEndUtc = x.PeriodEndUtc,
            Status = (short)x.Status,
            TotalAmount = x.TotalAmount,
            Currency = x.Currency,
            ClosedAtUtc = x.ClosedAtUtc,
            PaidAtUtc = x.PaidAtUtc,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
}
