using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<ShipmentRecord>
{
    public void Configure(EntityTypeBuilder<ShipmentRecord> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TrackingNumber).HasMaxLength(256);
        builder.Property(x => x.RawPayload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TrackingNumber);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
