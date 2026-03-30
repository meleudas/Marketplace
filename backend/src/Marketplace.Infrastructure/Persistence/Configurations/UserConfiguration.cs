using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API для доменної таблиці користувачів маркетплейсу.</summary>
public class UserConfiguration : IEntityTypeConfiguration<MarketplaceUserRecord>
{
    public void Configure(EntityTypeBuilder<MarketplaceUserRecord> builder)
    {
        builder.ToTable("marketplace_users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.Avatar).HasMaxLength(2048);
        builder.Property(x => x.VerificationDocument).HasMaxLength(2048);
        builder.Property(x => x.IsVerified).HasDefaultValue(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<MarketplaceUserRecord>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LastName);
        builder.HasIndex(x => x.FirstName);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
