using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Enums;

namespace Marketplace.Domain.Payments.Entities;

public sealed class Payment : AuditableSoftDeleteAggregateRoot<PaymentId>
{
    private Payment() { }

    public OrderId OrderId { get; private set; } = null!;
    public PaymentMethodKind PaymentMethod { get; private set; }
    public Money Amount { get; private set; } = Money.Zero;
    public string Currency { get; private set; } = "UAH";
    public string? TransactionId { get; private set; }
    public PaymentTransactionStatus Status { get; private set; }
    public JsonBlob ProviderResponse { get; private set; } = JsonBlob.Empty;
    public DateTime? ProcessedAt { get; private set; }

    public static Payment Create(
        PaymentId id,
        OrderId orderId,
        PaymentMethodKind paymentMethod,
        Money amount,
        string currency,
        string? transactionId,
        PaymentTransactionStatus status,
        JsonBlob providerResponse)
    {
        var now = DateTime.UtcNow;
        return new Payment
        {
            Id = id,
            OrderId = orderId,
            PaymentMethod = paymentMethod,
            Amount = amount,
            Currency = currency,
            TransactionId = string.IsNullOrWhiteSpace(transactionId) ? null : transactionId.Trim(),
            Status = status,
            ProviderResponse = providerResponse,
            ProcessedAt = status is PaymentTransactionStatus.Completed or PaymentTransactionStatus.Refunded ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public static Payment Reconstitute(
        PaymentId id,
        OrderId orderId,
        PaymentMethodKind paymentMethod,
        Money amount,
        string currency,
        string? transactionId,
        PaymentTransactionStatus status,
        JsonBlob providerResponse,
        DateTime? processedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            PaymentMethod = paymentMethod,
            Amount = amount,
            Currency = currency,
            TransactionId = transactionId,
            Status = status,
            ProviderResponse = providerResponse,
            ProcessedAt = processedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void UpdateProviderState(
        PaymentTransactionStatus status,
        string? transactionId,
        JsonBlob providerResponse,
        DateTime? processedAt = null)
    {
        Status = status;
        TransactionId = string.IsNullOrWhiteSpace(transactionId) ? TransactionId : transactionId.Trim();
        ProviderResponse = providerResponse;
        if (processedAt.HasValue)
            ProcessedAt = processedAt.Value;
        else if (status is PaymentTransactionStatus.Completed or PaymentTransactionStatus.Refunded)
            ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
