using Marketplace.Domain.Behavior.Entities;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class UserBehaviorDailyRepository : IUserBehaviorDailyRepository
{
    private readonly ApplicationDbContext _context;

    public UserBehaviorDailyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<UserBehaviorDaily?> GetAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        _ = userId;
        _ = date;
        return Task.FromResult<UserBehaviorDaily?>(null);
    }

    public Task<IReadOnlyList<UserBehaviorDaily>> ListRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        _ = from;
        _ = to;
        return Task.FromResult<IReadOnlyList<UserBehaviorDaily>>([]);
    }

    public Task<UserBehaviorDaily> UpsertAsync(UserBehaviorDaily entity, CancellationToken ct = default)
    {
        _ = ct;
        return Task.FromResult(entity);
    }
}
