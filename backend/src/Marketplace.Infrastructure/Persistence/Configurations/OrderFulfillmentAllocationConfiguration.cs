using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class OrderFulfillmentAllocationConfiguration : IEntityTypeConfiguration<OrderFulfillmentAllocationRecord>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentAllocationRecord> builder)
    {
        builder.ToTable("order_fulfillment_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.WarehouseId });
        builder.HasIndex(x => x.OrderItemId);
        builder.HasIndex(x => x.ReservationId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
