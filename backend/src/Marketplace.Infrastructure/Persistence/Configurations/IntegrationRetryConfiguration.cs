using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class IntegrationRetryConfiguration : IEntityTypeConfiguration<IntegrationRetryRecord>
{
    public void Configure(EntityTypeBuilder<IntegrationRetryRecord> builder)
    {
        builder.ToTable("integration_retries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AggregateType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AggregateId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.DeadLetterReason).HasMaxLength(2000);
        builder.Property(x => x.DeadLetterCategory).HasMaxLength(64);
        builder.HasIndex(x => new { x.Kind, x.AggregateType, x.AggregateId }).IsUnique();
        builder.HasIndex(x => x.NextAttemptAtUtc);
        builder.HasIndex(x => x.DeadLetteredAtUtc);
    }
}
