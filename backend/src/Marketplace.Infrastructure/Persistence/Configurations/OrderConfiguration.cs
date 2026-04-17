using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<OrderRecord>
{
    public void Configure(EntityTypeBuilder<OrderRecord> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TotalPrice).HasColumnType("numeric(14,2)");
        builder.Property(x => x.Subtotal).HasColumnType("numeric(14,2)");
        builder.Property(x => x.ShippingCost).HasColumnType("numeric(14,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.TaxAmount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.TrackingNumber).HasMaxLength(256);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Status);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
