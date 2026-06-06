using Marketplace.Domain.Behavior.Entities;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Behavior.Repositories;

public interface IBehaviorEventRepository
{
    Task<BehaviorEvent> AddAsync(BehaviorEvent entity, CancellationToken ct = default);
    Task<IReadOnlyList<BehaviorEvent>> ListRecentDuplicatesAsync(
        string eventKey,
        BehaviorEventType eventType,
        DateTime sinceUtc,
        CancellationToken ct = default);
    Task<int> CountByTypeAsync(BehaviorEventType eventType, DateTime sinceUtc, DateTime untilUtc, CancellationToken ct = default);
    Task SoftDeleteByUserIdAsync(Guid userId, DateTime deletedAtUtc, CancellationToken ct = default);
}
