using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class BehaviorDailyAggregateConfiguration : IEntityTypeConfiguration<BehaviorDailyAggregateRecord>
{
    public void Configure(EntityTypeBuilder<BehaviorDailyAggregateRecord> builder)
    {
        builder.ToTable("behavior_daily_aggregates");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.Date, x.EventType }).IsUnique();
    }
}
