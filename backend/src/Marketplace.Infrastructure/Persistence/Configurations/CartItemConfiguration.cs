using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItemRecord>
{
    public void Configure(EntityTypeBuilder<CartItemRecord> builder)
    {
        builder.ToTable("cart_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PriceAtMoment).HasColumnType("numeric(14,2)");
        builder.Property(x => x.Discount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
