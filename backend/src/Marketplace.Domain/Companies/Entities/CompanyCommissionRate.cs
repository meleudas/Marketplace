using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Companies.Entities;

public sealed class CompanyCommissionRate : AggregateRoot<CompanyCommissionRateId>
{
    private CompanyCommissionRate() { }

    public CompanyId CompanyId { get; private set; } = default!;
    public CompanyContractId ContractId { get; private set; } = default!;
    public decimal CommissionPercent { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public string? Reason { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static CompanyCommissionRate Create(
        CompanyCommissionRateId id,
        CompanyId companyId,
        CompanyContractId contractId,
        decimal commissionPercent,
        DateTime effectiveFrom,
        string? reason,
        Guid? createdByUserId)
    {
        ValidatePercent(commissionPercent);
        var now = DateTime.UtcNow;
        return new CompanyCommissionRate
        {
            Id = id,
            CompanyId = companyId,
            ContractId = contractId,
            CommissionPercent = commissionPercent,
            EffectiveFrom = effectiveFrom,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static CompanyCommissionRate Reconstitute(
        CompanyCommissionRateId id,
        CompanyId companyId,
        CompanyContractId contractId,
        decimal commissionPercent,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string? reason,
        Guid? createdByUserId,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            ContractId = contractId,
            CommissionPercent = commissionPercent,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Reason = reason,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void Close(DateTime effectiveTo)
    {
        if (effectiveTo <= EffectiveFrom)
            throw new DomainException("effectiveTo must be greater than effectiveFrom");

        EffectiveTo = effectiveTo;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidatePercent(decimal commissionPercent)
    {
        if (commissionPercent <= 0m || commissionPercent > 100m)
            throw new DomainException("Commission percent must be in range (0, 100]");
    }
}
