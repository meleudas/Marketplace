using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Domain.Companies.Entities;

public sealed class CompanySchedule : AuditableSoftDeleteAggregateRoot<CompanyScheduleId>
{
    private CompanySchedule() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public CompanyScheduleDay DayOfWeek { get; private set; }
    public TimeOnly OpenTime { get; private set; }
    public TimeOnly CloseTime { get; private set; }
    public bool IsClosed { get; private set; }

    public static CompanySchedule Reconstitute(
        CompanyScheduleId id,
        CompanyId companyId,
        CompanyScheduleDay dayOfWeek,
        TimeOnly openTime,
        TimeOnly closeTime,
        bool isClosed,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            DayOfWeek = dayOfWeek,
            OpenTime = openTime,
            CloseTime = closeTime,
            IsClosed = isClosed,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
