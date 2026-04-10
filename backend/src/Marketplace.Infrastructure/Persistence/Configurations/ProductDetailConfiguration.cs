using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetailRecord>
{
    public void Configure(EntityTypeBuilder<ProductDetailRecord> builder)
    {
        builder.ToTable("product_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AttributesRaw).HasColumnType("jsonb");
        builder.Property(x => x.VariantsRaw).HasColumnType("jsonb");
        builder.Property(x => x.SpecificationsRaw).HasColumnType("jsonb");
        builder.Property(x => x.SeoRaw).HasColumnType("jsonb");
        builder.Property(x => x.ContentBlocksRaw).HasColumnType("jsonb");
        builder.Property(x => x.Tags).HasColumnType("text[]");
        builder.Property(x => x.Brands).HasColumnType("text[]");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.ProductId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
