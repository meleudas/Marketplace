using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ReportActionConfiguration : IEntityTypeConfiguration<ReportActionRecord>
{
    public void Configure(EntityTypeBuilder<ReportActionRecord> builder)
    {
        builder.ToTable("report_actions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.ReportId, x.CreatedAt });
    }
}
