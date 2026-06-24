namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SellerPayoutRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public long SettlementBatchId { get; set; }
    public short Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? ProviderReference { get; set; }
    public string? Iban { get; set; }
    public string? RecipientName { get; set; }
    public DateTime? InitiatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
