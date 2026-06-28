using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItemRecord>
{
    public void Configure(EntityTypeBuilder<OrderItemRecord> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductImage).HasMaxLength(2048);
        builder.Property(x => x.PriceAtMoment).HasColumnType("numeric(14,2)");
        builder.Property(x => x.Discount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.TotalPrice).HasColumnType("numeric(14,2)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
