using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Behavior.Entities;

public sealed class UserBehaviorDaily : AuditableSoftDeleteAggregateRoot<UserBehaviorDailyId>
{
    private UserBehaviorDaily() { }

    public Guid UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public JsonBlob Metrics { get; private set; } = JsonBlob.Empty;
    public JsonBlob Preferences { get; private set; } = JsonBlob.Empty;
    public string? Notes { get; private set; }

    public static UserBehaviorDaily Reconstitute(
        UserBehaviorDailyId id,
        Guid userId,
        DateOnly date,
        JsonBlob metrics,
        JsonBlob preferences,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            Date = date,
            Metrics = metrics,
            Preferences = preferences,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
