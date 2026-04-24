using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CompanyReviewConfiguration : IEntityTypeConfiguration<CompanyReviewRecord>
{
    public void Configure(EntityTypeBuilder<CompanyReviewRecord> builder)
    {
        builder.ToTable("company_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RatingsRaw).HasColumnType("jsonb");
        builder.Property(x => x.OverallRating).HasColumnType("numeric(4,2)");
        builder.Property(x => x.Comment).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
