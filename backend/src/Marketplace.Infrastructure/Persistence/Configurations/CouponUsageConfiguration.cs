using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsageRecord>
{
    public void Configure(EntityTypeBuilder<CouponUsageRecord> builder)
    {
        builder.ToTable("coupon_usages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CouponCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DiscountAppliedAmount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CouponId, x.OrderId }).IsUnique();
        builder.HasIndex(x => new { x.CouponId, x.UserId });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
