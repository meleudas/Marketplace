using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Users.Enums;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Notifications;

public sealed class AdminNotificationRecipientIds : IAdminNotificationRecipientIds
{
    private readonly ApplicationDbContext _db;

    public AdminNotificationRecipientIds(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Guid>> ListAdminUserIdsAsync(CancellationToken ct = default)
    {
        var adminRole = (int)UserRole.Admin;
        var moderatorRole = (int)UserRole.Moderator;
        return await _db.MarketplaceUsers.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.Role == adminRole || x.Role == moderatorRole))
            .Select(x => x.Id)
            .Distinct()
            .ToListAsync(ct);
    }
}
