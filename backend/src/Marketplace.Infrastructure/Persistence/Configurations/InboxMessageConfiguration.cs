using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessageRecord>
{
    public void Configure(EntityTypeBuilder<InboxMessageRecord> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => new { x.MessageId, x.Consumer });
        builder.Property(x => x.Consumer).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Metadata).HasMaxLength(2000);
        builder.HasIndex(x => x.ProcessedAtUtc);
    }
}
