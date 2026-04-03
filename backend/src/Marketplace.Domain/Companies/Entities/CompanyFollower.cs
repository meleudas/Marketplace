using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Companies.Entities;

public sealed class CompanyFollower : AuditableSoftDeleteAggregateRoot<CompanyFollowerId>
{
    private CompanyFollower() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime FollowedAt { get; private set; }

    public static CompanyFollower Reconstitute(
        CompanyFollowerId id,
        CompanyId companyId,
        Guid userId,
        DateTime followedAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            UserId = userId,
            FollowedAt = followedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
