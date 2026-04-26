using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistoryRecord>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistoryRecord> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(256);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(x => new { x.OrderId, x.ChangedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
