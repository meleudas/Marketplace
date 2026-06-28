using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ReviewReplyConfiguration : IEntityTypeConfiguration<ReviewReplyRecord>
{
    public void Configure(EntityTypeBuilder<ReviewReplyRecord> builder)
    {
        builder.ToTable("review_replies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(x => x.ProductReviewId).IsUnique();
        builder.HasIndex(x => x.CompanyReviewId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
