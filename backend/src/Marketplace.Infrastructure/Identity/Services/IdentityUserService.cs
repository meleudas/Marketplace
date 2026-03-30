using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Identity.Entities;

namespace Marketplace.Infrastructure.Identity.Services;

/// <summary>Адаптер між доменними VO та сутністю ASP.NET Identity.</summary>
public class IdentityUserService
{
    public ApplicationUser CreateForRegistration(
        IdentityUserId identityId,
        Email email,
        UserName userName,
        string? phoneNumber)
    {
        return new ApplicationUser
        {
            Id = identityId.Value,
            UserName = userName.Value,
            Email = email.Value,
            EmailConfirmed = false,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
            PhoneNumberConfirmed = false,
            IsDeleted = false
        };
    }
}
