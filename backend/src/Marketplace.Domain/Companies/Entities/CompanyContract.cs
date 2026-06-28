using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Domain.Companies.Entities;

public sealed class CompanyContract : AggregateRoot<CompanyContractId>
{
    private CompanyContract() { }

    public CompanyId CompanyId { get; private set; } = default!;
    public string ContractNumber { get; private set; } = string.Empty;
    public CompanyContractStatus Status { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public DateTime? SignedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static CompanyContract CreateActive(
        CompanyContractId id,
        CompanyId companyId,
        string contractNumber,
        DateTime effectiveFrom,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(contractNumber))
            throw new DomainException("Contract number is required");

        var now = DateTime.UtcNow;
        return new CompanyContract
        {
            Id = id,
            CompanyId = companyId,
            ContractNumber = contractNumber.Trim(),
            Status = CompanyContractStatus.Active,
            EffectiveFrom = effectiveFrom,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static CompanyContract Reconstitute(
        CompanyContractId id,
        CompanyId companyId,
        string contractNumber,
        CompanyContractStatus status,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        DateTime? signedAt,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            ContractNumber = contractNumber,
            Status = status,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            SignedAt = signedAt,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
