using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Domain.Companies.Entities;

public sealed class CompanyLegalProfile : AggregateRoot<CompanyLegalProfileId>
{
    private CompanyLegalProfile() { }

    public CompanyId CompanyId { get; private set; } = default!;
    public string LegalName { get; private set; } = string.Empty;
    public CompanyLegalType LegalType { get; private set; }
    public string? Edrpou { get; private set; }
    public string? Ipn { get; private set; }
    public string? CertificateNumber { get; private set; }
    public bool IsVatPayer { get; private set; }
    public decimal? InitialCommissionPercent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static CompanyLegalProfile Create(
        CompanyLegalProfileId id,
        CompanyId companyId,
        string legalName,
        CompanyLegalType legalType,
        string? edrpou,
        string? ipn,
        string? certificateNumber,
        bool isVatPayer,
        decimal? initialCommissionPercent)
    {
        Validate(legalName, legalType, edrpou, ipn, initialCommissionPercent);
        var now = DateTime.UtcNow;
        return new CompanyLegalProfile
        {
            Id = id,
            CompanyId = companyId,
            LegalName = legalName.Trim(),
            LegalType = legalType,
            Edrpou = Normalize(edrpou),
            Ipn = Normalize(ipn),
            CertificateNumber = Normalize(certificateNumber),
            IsVatPayer = isVatPayer,
            InitialCommissionPercent = initialCommissionPercent,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static CompanyLegalProfile Reconstitute(
        CompanyLegalProfileId id,
        CompanyId companyId,
        string legalName,
        CompanyLegalType legalType,
        string? edrpou,
        string? ipn,
        string? certificateNumber,
        bool isVatPayer,
        decimal? initialCommissionPercent,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            LegalName = legalName,
            LegalType = legalType,
            Edrpou = edrpou,
            Ipn = ipn,
            CertificateNumber = certificateNumber,
            IsVatPayer = isVatPayer,
            InitialCommissionPercent = initialCommissionPercent,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    private static void Validate(string legalName, CompanyLegalType legalType, string? edrpou, string? ipn, decimal? initialCommissionPercent)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("Legal name is required");

        if ((legalType == CompanyLegalType.Llc || legalType == CompanyLegalType.Jsc) && string.IsNullOrWhiteSpace(edrpou))
            throw new DomainException("EDRPOU is required for legal entities");

        if ((legalType == CompanyLegalType.Individual || legalType == CompanyLegalType.Entrepreneur) && string.IsNullOrWhiteSpace(ipn))
            throw new DomainException("IPN is required for individual or entrepreneur");

        if (initialCommissionPercent.HasValue && (initialCommissionPercent.Value <= 0m || initialCommissionPercent.Value > 100m))
            throw new DomainException("Initial commission percent must be in range (0, 100]");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
