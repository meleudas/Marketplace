using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ShippingQuoteConfiguration : IEntityTypeConfiguration<ShippingQuoteRecord>
{
    public void Configure(EntityTypeBuilder<ShippingQuoteRecord> builder)
    {
        builder.ToTable("shipping_quotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("numeric(14,2)");
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
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
