using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Entities;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class SellerPayoutRepository : ISellerPayoutRepository
{
    private readonly ApplicationDbContext _context;

    public SellerPayoutRepository(ApplicationDbContext context) => _context = context;

    public async Task<SellerPayout?> GetByIdAsync(SellerPayoutId id, CancellationToken ct = default)
    {
        var row = await _context.SellerPayouts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<SellerPayout?> GetBySettlementBatchIdAsync(SettlementBatchId settlementBatchId, CancellationToken ct = default)
    {
        var row = await _context.SellerPayouts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettlementBatchId == settlementBatchId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<SellerPayout>> ListByStatusAsync(SellerPayoutStatus status, CancellationToken ct = default)
    {
        var rows = await _context.SellerPayouts.AsNoTracking()
            .Where(x => x.Status == (short)status)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<SellerPayout> AddAsync(SellerPayout payout, CancellationToken ct = default)
    {
        var row = ToRecord(payout);
        await _context.SellerPayouts.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(SellerPayout payout, CancellationToken ct = default)
    {
        var row = await _context.SellerPayouts.FirstOrDefaultAsync(x => x.Id == payout.Id.Value, ct)
            ?? throw new InvalidOperationException($"Seller payout '{payout.Id.Value}' was not found.");

        row.Status = (short)payout.Status;
        row.ProviderReference = payout.ProviderReference;
        row.InitiatedAtUtc = payout.InitiatedAtUtc;
        row.CompletedAtUtc = payout.CompletedAtUtc;
        row.FailureReason = payout.FailureReason;
        row.UpdatedAt = payout.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static SellerPayout ToDomain(SellerPayoutRecord row) =>
        SellerPayout.Reconstitute(
            SellerPayoutId.From(row.Id),
            CompanyId.From(row.CompanyId),
            SettlementBatchId.From(row.SettlementBatchId),
            (SellerPayoutStatus)row.Status,
            row.Amount,
            row.Currency,
            row.ProviderReference,
            row.Iban,
            row.RecipientName,
            row.InitiatedAtUtc,
            row.CompletedAtUtc,
            row.FailureReason,
            row.CreatedAt,
            row.UpdatedAt);

    private static SellerPayoutRecord ToRecord(SellerPayout x) =>
        new()
        {
            Id = x.Id.Value,
            CompanyId = x.CompanyId.Value,
            SettlementBatchId = x.SettlementBatchId.Value,
            Status = (short)x.Status,
            Amount = x.Amount,
            Currency = x.Currency,
            ProviderReference = x.ProviderReference,
            Iban = x.Iban,
            RecipientName = x.RecipientName,
            InitiatedAtUtc = x.InitiatedAtUtc,
            CompletedAtUtc = x.CompletedAtUtc,
            FailureReason = x.FailureReason,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
}
