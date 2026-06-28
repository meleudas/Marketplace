using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly ApplicationDbContext _db;

    public PushSubscriptionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task UpsertAsync(
        Guid userId,
        string endpoint,
        string p256dh,
        string auth,
        PushSubscriptionAudienceFlags audienceFlags,
        string? userAgent,
        CancellationToken ct = default)
    {
        var existing = await _db.PushSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint, ct);
        var now = DateTime.UtcNow;
        var flags = (int)audienceFlags;

        if (existing is not null)
        {
            if (existing.UserId != userId)
            {
                var row = await _db.PushSubscriptions.FirstAsync(x => x.Id == existing.Id, ct);
                _db.PushSubscriptions.Remove(row);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                var row = await _db.PushSubscriptions.FirstAsync(x => x.Id == existing.Id, ct);
                row.P256dh = p256dh;
                row.Auth = auth;
                row.AudienceFlags = flags;
                row.UserAgent = userAgent;
                await _db.SaveChangesAsync(ct);
                return;
            }
        }

        await _db.PushSubscriptions.AddAsync(new PushSubscriptionRecord
        {
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            AudienceFlags = flags,
            UserAgent = userAgent,
            CreatedAtUtc = now
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByUserAndEndpointAsync(Guid userId, string endpoint, CancellationToken ct = default)
    {
        await _db.PushSubscriptions.Where(x => x.UserId == userId && x.Endpoint == endpoint)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<PushSubscriptionDto>> ListByUserAndAudienceAsync(
        Guid userId,
        PushSubscriptionAudienceFlags requiredFlags,
        CancellationToken ct = default)
    {
        var mask = (int)requiredFlags;
        return await _db.PushSubscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && (x.AudienceFlags & mask) == mask)
            .Select(x => new PushSubscriptionDto(x.Id, x.UserId, x.Endpoint, x.P256dh, x.Auth,
                (PushSubscriptionAudienceFlags)x.AudienceFlags))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PushSubscriptionDto>> ListByAudienceFlagAsync(
        PushSubscriptionAudienceFlags requiredFlag,
        CancellationToken ct = default)
    {
        var bit = (int)requiredFlag;
        return await _db.PushSubscriptions.AsNoTracking()
            .Where(x => (x.AudienceFlags & bit) != 0)
            .Select(x => new PushSubscriptionDto(x.Id, x.UserId, x.Endpoint, x.P256dh, x.Auth,
                (PushSubscriptionAudienceFlags)x.AudienceFlags))
            .ToListAsync(ct);
    }

    public async Task DeleteByIdAsync(long id, CancellationToken ct = default)
    {
        await _db.PushSubscriptions.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }
}
