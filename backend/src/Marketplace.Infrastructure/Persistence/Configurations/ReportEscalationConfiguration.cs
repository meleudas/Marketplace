using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ReportEscalationConfiguration : IEntityTypeConfiguration<ReportEscalationRecord>
{
    public void Configure(EntityTypeBuilder<ReportEscalationRecord> builder)
    {
        builder.ToTable("report_escalations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EscalatedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.ReportId, x.CreatedAt });
    }
}
