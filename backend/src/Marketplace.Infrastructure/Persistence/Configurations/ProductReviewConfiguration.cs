using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReviewRecord>
{
    public void Configure(EntityTypeBuilder<ProductReviewRecord> builder)
    {
        builder.ToTable("product_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.UserAvatar).HasMaxLength(2048);
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Comment).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ImagesRaw).HasColumnType("jsonb");
        builder.Property(x => x.ProsRaw).HasColumnType("jsonb");
        builder.Property(x => x.ConsRaw).HasColumnType("jsonb");
        builder.Property(x => x.HelpfulRaw).HasColumnType("jsonb");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.ProductId, x.Status });
        builder.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
