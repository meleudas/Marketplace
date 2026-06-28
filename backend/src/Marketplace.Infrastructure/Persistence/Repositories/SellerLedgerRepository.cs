using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SellerLedgerRepository : ISellerLedgerRepository
{
    private readonly ApplicationDbContext _context;

    public SellerLedgerRepository(ApplicationDbContext context) => _context = context;

    public async Task<SellerLedgerEntry?> GetByIdAsync(SellerLedgerEntryId id, CancellationToken ct = default)
    {
        var row = await _context.SellerLedgerEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public Task<bool> ExistsForOrderAndTypeAsync(
        OrderId orderId,
        SellerLedgerEntryType entryType,
        CancellationToken ct = default) =>
        _context.SellerLedgerEntries.AsNoTracking()
            .AnyAsync(x => x.OrderId == orderId.Value && x.EntryType == (short)entryType, ct);

    public async Task<IReadOnlyList<SellerLedgerEntry>> ListByCompanyIdAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var rows = await _context.SellerLedgerEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SellerLedgerEntry>> ListAvailableForSettlementAsync(
        CompanyId companyId,
        DateTime asOfUtc,
        CancellationToken ct = default)
    {
        var rows = await _context.SellerLedgerEntries
            .Where(x => x.CompanyId == companyId.Value
                && x.Status == (short)SellerLedgerEntryStatus.Pending
                && x.EntryType == (short)SellerLedgerEntryType.Sale
                && x.AvailableAtUtc != null
                && x.AvailableAtUtc <= asOfUtc)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SellerLedgerEntry>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.SellerLedgerEntries.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<CompanyId>> ListCompanyIdsWithPendingSaleEntriesAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        var ids = await _context.SellerLedgerEntries.AsNoTracking()
            .Where(x => x.Status == (short)SellerLedgerEntryStatus.Pending
                && x.EntryType == (short)SellerLedgerEntryType.Sale
                && x.AvailableAtUtc != null
                && x.AvailableAtUtc <= asOfUtc)
            .Select(x => x.CompanyId)
            .Distinct()
            .ToListAsync(ct);
        return ids.Select(CompanyId.From).ToList();
    }

    public async Task<SellerLedgerEntry> AddAsync(SellerLedgerEntry entry, CancellationToken ct = default)
    {
        var row = ToRecord(entry);
        await _context.SellerLedgerEntries.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task AddRangeAsync(IReadOnlyList<SellerLedgerEntry> entries, CancellationToken ct = default)
    {
        await _context.SellerLedgerEntries.AddRangeAsync(entries.Select(ToRecord), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SellerLedgerEntry entry, CancellationToken ct = default)
    {
        var row = await _context.SellerLedgerEntries.FirstOrDefaultAsync(x => x.Id == entry.Id.Value, ct)
            ?? throw new InvalidOperationException($"Seller ledger entry '{entry.Id.Value}' was not found.");

        Apply(row, entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IReadOnlyList<SellerLedgerEntry> entries, CancellationToken ct = default)
    {
        var ids = entries.Select(x => x.Id.Value).ToList();
        var rows = await _context.SellerLedgerEntries.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        foreach (var entry in entries)
        {
            var row = rows.FirstOrDefault(x => x.Id == entry.Id.Value)
                ?? throw new InvalidOperationException($"Seller ledger entry '{entry.Id.Value}' was not found.");
            Apply(row, entry);
        }

        await _context.SaveChangesAsync(ct);
    }

    private static void Apply(SellerLedgerEntryRecord row, SellerLedgerEntry entry)
    {
        row.Status = (short)entry.Status;
        row.SettlementBatchId = entry.SettlementBatchId?.Value;
        row.SellerPayoutId = entry.SellerPayoutId?.Value;
        row.AvailableAtUtc = entry.AvailableAtUtc;
        row.SettledAtUtc = entry.SettledAtUtc;
        row.UpdatedAt = entry.UpdatedAt;
    }

    private static SellerLedgerEntry ToDomain(SellerLedgerEntryRecord row) =>
        SellerLedgerEntry.Reconstitute(
            SellerLedgerEntryId.From(row.Id),
            CompanyId.From(row.CompanyId),
            OrderId.From(row.OrderId),
            row.OrderFinancialsId.HasValue ? OrderFinancialsId.From(row.OrderFinancialsId.Value) : null,
            row.SettlementBatchId.HasValue ? SettlementBatchId.From(row.SettlementBatchId.Value) : null,
            row.SellerPayoutId.HasValue ? SellerPayoutId.From(row.SellerPayoutId.Value) : null,
            (SellerLedgerEntryType)row.EntryType,
            (SellerLedgerEntryStatus)row.Status,
            row.Amount,
            row.Currency,
            row.Description,
            row.AvailableAtUtc,
            row.SettledAtUtc,
            row.CreatedAt,
            row.UpdatedAt);

    private static SellerLedgerEntryRecord ToRecord(SellerLedgerEntry x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            OrderId = x.OrderId.Value,
            OrderFinancialsId = x.OrderFinancialsId?.Value,
            SettlementBatchId = x.SettlementBatchId?.Value,
            SellerPayoutId = x.SellerPayoutId?.Value,
            EntryType = (short)x.EntryType,
            Status = (short)x.Status,
            Amount = x.Amount,
            Currency = x.Currency,
            Description = x.Description,
            AvailableAtUtc = x.AvailableAtUtc,
            SettledAtUtc = x.SettledAtUtc,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
}
