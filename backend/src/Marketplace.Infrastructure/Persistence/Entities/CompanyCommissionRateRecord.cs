namespace Marketplace.Infrastructure.Persistence.Entities;

public class CompanyCommissionRateRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public long ContractId { get; set; }
    public decimal CommissionPercent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Reason { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
