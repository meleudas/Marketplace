using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImageRecord>
{
    public void Configure(EntityTypeBuilder<ProductImageRecord> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.OriginalObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ImageObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ThumbnailObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(512).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
        builder.HasIndex(x => x.ProductId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
