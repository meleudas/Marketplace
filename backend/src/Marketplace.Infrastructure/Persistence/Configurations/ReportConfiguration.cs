using Marketplace.Domain.Reports.Enums;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<ReportRecord>
{
    public void Configure(EntityTypeBuilder<ReportRecord> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReporterUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TargetId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Images).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.AssignedModeratorId).HasMaxLength(64);
        builder.Property(x => x.ReviewedById).HasMaxLength(64);
        builder.Property(x => x.ClosedById).HasMaxLength(64);
        builder.Property(x => x.LastActionReason).HasMaxLength(2000);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.Status, x.Priority, x.CreatedAt });
        builder.HasIndex(x => new { x.ReporterUserId, x.TargetType, x.TargetId, x.Reason, x.CreatedAt });

        builder.HasQueryFilter(x => !x.IsDeleted && x.Status != (short)ReportStatus.Closed);
    }
}
