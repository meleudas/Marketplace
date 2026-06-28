using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Enums;

namespace Marketplace.Domain.Finance.Entities;

public sealed class SellerPayout : AggregateRoot<SellerPayoutId>
{
    private SellerPayout() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public SettlementBatchId SettlementBatchId { get; private set; } = null!;
    public SellerPayoutStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "UAH";
    public string? ProviderReference { get; private set; }
    public string? Iban { get; private set; }
    public string? RecipientName { get; private set; }
    public DateTime? InitiatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static SellerPayout CreatePending(
        SellerPayoutId id,
        CompanyId companyId,
        SettlementBatchId settlementBatchId,
        decimal amount,
        string currency,
        string? iban,
        string? recipientName)
    {
        if (amount <= 0)
            throw new DomainException("Payout amount must be positive");

        var now = DateTime.UtcNow;
        return new SellerPayout
        {
            Id = id,
            CompanyId = companyId,
            SettlementBatchId = settlementBatchId,
            Status = SellerPayoutStatus.Pending,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "UAH" : currency.Trim().ToUpperInvariant(),
            Iban = string.IsNullOrWhiteSpace(iban) ? null : iban.Trim(),
            RecipientName = string.IsNullOrWhiteSpace(recipientName) ? null : recipientName.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SellerPayout Reconstitute(
        SellerPayoutId id,
        CompanyId companyId,
        SettlementBatchId settlementBatchId,
        SellerPayoutStatus status,
        decimal amount,
        string currency,
        string? providerReference,
        string? iban,
        string? recipientName,
        DateTime? initiatedAtUtc,
        DateTime? completedAtUtc,
        string? failureReason,
        DateTime createdAt,
        DateTime updatedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            SettlementBatchId = settlementBatchId,
            Status = status,
            Amount = amount,
            Currency = currency,
            ProviderReference = providerReference,
            Iban = iban,
            RecipientName = recipientName,
            InitiatedAtUtc = initiatedAtUtc,
            CompletedAtUtc = completedAtUtc,
            FailureReason = failureReason,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

    public void MarkProcessing(string? providerReference)
    {
        if (Status != SellerPayoutStatus.Pending)
            throw new DomainException("Only pending payouts can enter processing");

        Status = SellerPayoutStatus.Processing;
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim();
        InitiatedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaid(string? providerReference)
    {
        Status = SellerPayoutStatus.Paid;
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = SellerPayoutStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Payout failed" : reason.Trim();
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
