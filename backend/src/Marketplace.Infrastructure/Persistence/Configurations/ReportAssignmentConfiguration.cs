using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ReportAssignmentConfiguration : IEntityTypeConfiguration<ReportAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<ReportAssignmentRecord> builder)
    {
        builder.ToTable("report_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ModeratorUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AssignedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.ReportId, x.CreatedAt });
    }
}
