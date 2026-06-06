using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ShippingEventConfiguration : IEntityTypeConfiguration<ShippingEventRecord>
{
    public void Configure(EntityTypeBuilder<ShippingEventRecord> builder)
    {
        builder.ToTable("shipping_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RawPayload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CarrierCode, x.EventKey, x.PayloadHash }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
