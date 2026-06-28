namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SellerLedgerEntryRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public long OrderId { get; set; }
    public long? OrderFinancialsId { get; set; }
    public long? SettlementBatchId { get; set; }
    public long? SellerPayoutId { get; set; }
    public short EntryType { get; set; }
    public short Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? Description { get; set; }
    public DateTime? AvailableAtUtc { get; set; }
    public DateTime? SettledAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
