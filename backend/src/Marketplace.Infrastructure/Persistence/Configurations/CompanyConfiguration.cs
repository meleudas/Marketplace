using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<CompanyRecord>
{
    public void Configure(EntityTypeBuilder<CompanyRecord> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.ContactEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContactPhone).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AddressStreet).HasMaxLength(255).IsRequired();
        builder.Property(x => x.AddressCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AddressState).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AddressPostalCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AddressCountry).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(64);
        builder.Property(x => x.MetaRaw).HasColumnType("jsonb");

        builder.Property(x => x.IsApproved).HasDefaultValue(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.ReviewCount).HasDefaultValue(0);
        builder.Property(x => x.FollowerCount).HasDefaultValue(0);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.IsApproved);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
