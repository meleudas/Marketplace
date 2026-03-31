using Marketplace.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.TelegramChatId).HasMaxLength(64);
        builder.Property(x => x.TelegramTwoFactorEnabled).HasDefaultValue(false);
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
