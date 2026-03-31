using Microsoft.AspNetCore.Identity;

namespace Marketplace.Infrastructure.Identity.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsDeleted { get; set; }
    public string? TelegramChatId { get; set; }
    public bool TelegramTwoFactorEnabled { get; set; }
    public DateTime? TelegramLinkedAtUtc { get; set; }
}
