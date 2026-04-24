namespace Marketplace.Infrastructure.Persistence.Entities;

public class CompanyLegalProfileRecord
{
    public long Id { get; set; }
    public Guid CompanyId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public short LegalType { get; set; }
    public string? Edrpou { get; set; }
    public string? Ipn { get; set; }
    public string? CertificateNumber { get; set; }
    public bool IsVatPayer { get; set; }
    public decimal? InitialCommissionPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
