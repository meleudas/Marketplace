using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<CouponRecord>
{
    public void Configure(EntityTypeBuilder<CouponRecord> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.MinOrderAmount).HasColumnType("numeric(14,2)");
        builder.Property(x => x.ApplicableCategoriesRaw).HasColumnType("jsonb");
        builder.Property(x => x.ApplicableProductsRaw).HasColumnType("jsonb");
        builder.Property(x => x.ApplicableCompaniesRaw).HasColumnType("jsonb");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.StartsAtUtc, x.ExpiresAtUtc });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
