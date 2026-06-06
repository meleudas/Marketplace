using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class BehaviorEventRawConfiguration : IEntityTypeConfiguration<BehaviorEventRawRecord>
{
    public void Configure(EntityTypeBuilder<BehaviorEventRawRecord> builder)
    {
        builder.ToTable("behavior_events_raw");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(x => new { x.EventType, x.OccurredAtUtc });
        builder.HasIndex(x => x.EventKey);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
