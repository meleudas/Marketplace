using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Notifications;

public sealed class AppNotificationUserDirectory : IAppNotificationUserContactReader
{
    private readonly UserManager<ApplicationUser> _users;

    public AppNotificationUserDirectory(UserManager<ApplicationUser> users) => _users = users;

    public async Task<AppNotificationUserContact?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var u = await _users.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (u is null || u.IsDeleted)
            return null;

        return new AppNotificationUserContact(
            u.Email,
            u.EmailConfirmed,
            u.TelegramChatId,
            u.NotifyAppByEmail,
            u.NotifyAppByTelegram);
    }
}
