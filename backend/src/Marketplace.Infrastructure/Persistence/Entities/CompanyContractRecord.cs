namespace Marketplace.Infrastructure.Persistence.Entities;

public class CompanyContractRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public short Status { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
