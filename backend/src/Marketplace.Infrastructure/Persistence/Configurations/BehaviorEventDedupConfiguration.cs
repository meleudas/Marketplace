using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class BehaviorEventDedupConfiguration : IEntityTypeConfiguration<BehaviorEventDedupRecord>
{
    public void Configure(EntityTypeBuilder<BehaviorEventDedupRecord> builder)
    {
        builder.ToTable("behavior_events_dedup");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.EventKey, x.EventType, x.BucketStartedAtUtc }).IsUnique();
    }
}
