using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context) => _context = context;

    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default)
    {
        var row = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        var row = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default)
    {
        var normalized = transactionId.Trim();
        var row = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.TransactionId == normalized, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Payment>> ListByStatusAsync(PaymentTransactionStatus status, CancellationToken ct = default)
    {
        var rows = await _context.Payments.AsNoTracking().Where(x => x.Status == (short)status).ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken ct = default)
    {
        var row = ToRecord(payment);
        await _context.Payments.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        var row = await _context.Payments.FirstOrDefaultAsync(x => x.Id == payment.Id.Value, ct)
            ?? throw new InvalidOperationException($"Payment '{payment.Id.Value}' was not found.");

        row.TransactionId = payment.TransactionId;
        row.Status = (short)payment.Status;
        row.ProviderResponseRaw = payment.ProviderResponse.Raw;
        row.ProcessedAt = payment.ProcessedAt;
        row.UpdatedAt = payment.UpdatedAt;
        row.IsDeleted = payment.IsDeleted;
        row.DeletedAt = payment.DeletedAt;

        await _context.SaveChangesAsync(ct);
    }

    private static Payment ToDomain(PaymentRecord r) =>
        Payment.Reconstitute(
            PaymentId.From(r.Id),
            OrderId.From(r.OrderId),
            (PaymentMethodKind)r.PaymentMethod,
            new Money(r.Amount),
            r.Currency,
            r.TransactionId,
            (PaymentTransactionStatus)r.Status,
            new JsonBlob(r.ProviderResponseRaw),
            r.ProcessedAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static PaymentRecord ToRecord(Payment x) =>
        new()
        {
            Id = x.Id.Value,
            OrderId = x.OrderId.Value,
            PaymentMethod = (short)x.PaymentMethod,
            Amount = x.Amount.Amount,
            Currency = x.Currency,
            TransactionId = x.TransactionId,
            Status = (short)x.Status,
            ProviderResponseRaw = x.ProviderResponse.Raw,
            ProcessedAt = x.ProcessedAt,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt
        };
}
