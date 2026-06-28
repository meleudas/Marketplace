using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItemRecord>
{
    public void Configure(EntityTypeBuilder<ShipmentItemRecord> builder)
    {
        builder.ToTable("shipment_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).IsRequired();
        builder.HasIndex(x => new { x.ShipmentId, x.OrderItemId }).IsUnique();
        builder.HasIndex(x => x.OrderItemId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
