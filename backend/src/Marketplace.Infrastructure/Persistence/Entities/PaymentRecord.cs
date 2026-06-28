namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class PaymentRecord
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public short PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? TransactionId { get; set; }
    public short Status { get; set; }
    public string? ProviderResponseRaw { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
