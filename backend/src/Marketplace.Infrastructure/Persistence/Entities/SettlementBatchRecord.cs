namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class SettlementBatchRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public short Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "UAH";
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
