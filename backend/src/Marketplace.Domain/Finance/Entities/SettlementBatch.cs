using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Enums;

namespace Marketplace.Domain.Finance.Entities;

public sealed class SettlementBatch : AggregateRoot<SettlementBatchId>
{
    private SettlementBatch() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public DateTime PeriodStartUtc { get; private set; }
    public DateTime PeriodEndUtc { get; private set; }
    public SettlementBatchStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "UAH";
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static SettlementBatch CreateOpen(
        SettlementBatchId id,
        CompanyId companyId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        string currency)
    {
        if (periodEndUtc <= periodStartUtc)
            throw new DomainException("Settlement period end must be after start");

        var now = DateTime.UtcNow;
        return new SettlementBatch
        {
            Id = id,
            CompanyId = companyId,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
            Status = SettlementBatchStatus.Open,
            TotalAmount = 0m,
            Currency = string.IsNullOrWhiteSpace(currency) ? "UAH" : currency.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SettlementBatch Reconstitute(
        SettlementBatchId id,
        CompanyId companyId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        SettlementBatchStatus status,
        decimal totalAmount,
        string currency,
        DateTime? closedAtUtc,
        DateTime? paidAtUtc,
        DateTime createdAt,
        DateTime updatedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
            Status = status,
            TotalAmount = totalAmount,
            Currency = currency,
            ClosedAtUtc = closedAtUtc,
            PaidAtUtc = paidAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

    public void AddAmount(decimal amount)
    {
        if (Status != SettlementBatchStatus.Open)
            throw new DomainException("Cannot add amount to a closed settlement batch");

        TotalAmount += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReady(DateTime closedAtUtc)
    {
        if (Status != SettlementBatchStatus.Open)
            throw new DomainException("Only open batches can be marked ready");

        Status = SettlementBatchStatus.Ready;
        ClosedAtUtc = closedAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkProcessing()
    {
        if (Status != SettlementBatchStatus.Ready)
            throw new DomainException("Only ready batches can enter processing");

        Status = SettlementBatchStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaid(DateTime paidAtUtc)
    {
        Status = SettlementBatchStatus.Paid;
        PaidAtUtc = paidAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = SettlementBatchStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}
