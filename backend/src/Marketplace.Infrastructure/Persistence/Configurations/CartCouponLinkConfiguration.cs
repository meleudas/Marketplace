using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CartCouponLinkConfiguration : IEntityTypeConfiguration<CartCouponLinkRecord>
{
    public void Configure(EntityTypeBuilder<CartCouponLinkRecord> builder)
    {
        builder.ToTable("cart_coupon_links");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CouponCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ValidationSnapshotRaw).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.CartId).IsUnique();
        builder.HasIndex(x => x.CouponId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
