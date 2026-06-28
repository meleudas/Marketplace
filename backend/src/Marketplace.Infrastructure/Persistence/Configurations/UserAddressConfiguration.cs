using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddressRecord>
{
    public void Configure(EntityTypeBuilder<UserAddressRecord> builder)
    {
        builder.ToTable("user_addresses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(255).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefault })
            .HasFilter("\"IsDefault\" = true AND \"IsDeleted\" = false");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
