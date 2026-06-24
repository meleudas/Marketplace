using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Enums;

namespace Marketplace.Domain.Finance.Entities;

public sealed class SellerLedgerEntry : AggregateRoot<SellerLedgerEntryId>
{
    private SellerLedgerEntry() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public OrderId OrderId { get; private set; } = null!;
    public OrderFinancialsId? OrderFinancialsId { get; private set; }
    public SettlementBatchId? SettlementBatchId { get; private set; }
    public SellerPayoutId? SellerPayoutId { get; private set; }
    public SellerLedgerEntryType EntryType { get; private set; }
    public SellerLedgerEntryStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "UAH";
    public string? Description { get; private set; }
    public DateTime? AvailableAtUtc { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static SellerLedgerEntry Create(
        SellerLedgerEntryId id,
        CompanyId companyId,
        OrderId orderId,
        OrderFinancialsId? orderFinancialsId,
        SellerLedgerEntryType entryType,
        decimal amount,
        string currency,
        string? description,
        DateTime? availableAtUtc)
    {
        if (amount == 0)
            throw new DomainException("Ledger entry amount cannot be zero");

        var now = DateTime.UtcNow;
        return new SellerLedgerEntry
        {
            Id = id,
            CompanyId = companyId,
            OrderId = orderId,
            OrderFinancialsId = orderFinancialsId,
            EntryType = entryType,
            Status = SellerLedgerEntryStatus.Pending,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "UAH" : currency.Trim().ToUpperInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            AvailableAtUtc = availableAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SellerLedgerEntry Reconstitute(
        SellerLedgerEntryId id,
        CompanyId companyId,
        OrderId orderId,
        OrderFinancialsId? orderFinancialsId,
        SettlementBatchId? settlementBatchId,
        SellerPayoutId? sellerPayoutId,
        SellerLedgerEntryType entryType,
        SellerLedgerEntryStatus status,
        decimal amount,
        string currency,
        string? description,
        DateTime? availableAtUtc,
        DateTime? settledAtUtc,
        DateTime createdAt,
        DateTime updatedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            OrderId = orderId,
            OrderFinancialsId = orderFinancialsId,
            SettlementBatchId = settlementBatchId,
            SellerPayoutId = sellerPayoutId,
            EntryType = entryType,
            Status = status,
            Amount = amount,
            Currency = currency,
            Description = description,
            AvailableAtUtc = availableAtUtc,
            SettledAtUtc = settledAtUtc,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

    public void MarkAvailable(DateTime availableAtUtc)
    {
        if (Status != SellerLedgerEntryStatus.Pending)
            throw new DomainException("Only pending entries can become available");

        Status = SellerLedgerEntryStatus.Available;
        AvailableAtUtc = availableAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToSettlementBatch(SettlementBatchId batchId)
    {
        if (Status != SellerLedgerEntryStatus.Available)
            throw new DomainException("Only available entries can be assigned to a settlement batch");

        SettlementBatchId = batchId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSettled(DateTime settledAtUtc)
    {
        Status = SellerLedgerEntryStatus.Settled;
        SettledAtUtc = settledAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToPayout(SellerPayoutId payoutId)
    {
        SellerPayoutId = payoutId;
        UpdatedAt = DateTime.UtcNow;
    }
}
