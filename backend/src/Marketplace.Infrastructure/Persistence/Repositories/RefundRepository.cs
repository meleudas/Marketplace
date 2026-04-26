using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class RefundRepository : IRefundRepository
{
    private readonly ApplicationDbContext _context;

    public RefundRepository(ApplicationDbContext context) => _context = context;

    public async Task<Refund?> GetByIdAsync(RefundId id, CancellationToken ct = default)
    {
        var row = await _context.Refunds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Refund>> ListByStatusAsync(RefundStatus status, CancellationToken ct = default)
    {
        var rows = await _context.Refunds.AsNoTracking().Where(x => x.Status == (short)status).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Refund>> ListByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var rows = await _context.Refunds.AsNoTracking()
            .Where(x => x.OrderId == orderId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Refund> AddAsync(Refund refund, CancellationToken ct = default)
    {
        var row = ToRecord(refund);
        await _context.Refunds.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Refund refund, CancellationToken ct = default)
    {
        var row = await _context.Refunds.FirstOrDefaultAsync(x => x.Id == refund.Id.Value, ct)
            ?? throw new InvalidOperationException($"Refund '{refund.Id.Value}' was not found.");
        row.Status = (short)refund.Status;
        row.ProcessedAt = refund.ProcessedAt;
        row.UpdatedAt = refund.UpdatedAt;
        row.IsDeleted = refund.IsDeleted;
        row.DeletedAt = refund.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static Refund ToDomain(RefundRecord r) =>
        Refund.Reconstitute(
            RefundId.From(r.Id),
            PaymentId.From(r.PaymentId),
            OrderId.From(r.OrderId),
            new Money(r.Amount),
            r.Reason,
            (RefundStatus)r.Status,
            r.ProcessedByUserId,
            r.ProcessedAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static RefundRecord ToRecord(Refund x) =>
        new()
        {
            Id = x.Id.Value,
            PaymentId = x.PaymentId.Value,
            OrderId = x.OrderId.Value,
            Amount = x.Amount.Amount,
            Reason = x.Reason,
            Status = (short)x.Status,
            ProcessedByUserId = x.ProcessedByUserId,
            ProcessedAt = x.ProcessedAt,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
