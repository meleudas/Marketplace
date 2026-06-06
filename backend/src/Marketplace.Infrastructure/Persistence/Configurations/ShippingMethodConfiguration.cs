using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethodRecord>
{
    public void Configure(EntityTypeBuilder<ShippingMethodRecord> builder)
    {
        builder.ToTable("shipping_methods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Price).HasColumnType("numeric(14,2)");
        builder.Property(x => x.FreeShippingThreshold).HasColumnType("numeric(14,2)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.IsDeleted });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
