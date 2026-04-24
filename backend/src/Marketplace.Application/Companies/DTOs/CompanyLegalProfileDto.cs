namespace Marketplace.Application.Companies.DTOs;

public sealed record CompanyLegalProfileDto(
    string LegalName,
    string LegalType,
    string? Edrpou,
    string? Ipn,
    string? CertificateNumber,
    bool IsVatPayer,
    decimal? InitialCommissionPercent);
