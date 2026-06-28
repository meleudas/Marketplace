using Marketplace.Domain.Behavior.Entities;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Behavior.Repositories;

public interface IUserBehaviorDailyRepository
{
    Task<UserBehaviorDaily?> GetAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<UserBehaviorDaily>> ListRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<UserBehaviorDaily> UpsertAsync(UserBehaviorDaily entity, CancellationToken ct = default);
}
