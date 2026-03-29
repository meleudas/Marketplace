using Microsoft.AspNetCore.Identity;

namespace Marketplace.Infrastructure.Identity.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsDeleted { get; set; }
}
