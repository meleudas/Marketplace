using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketEventConfiguration : IEntityTypeConfiguration<SupportTicketEventRecord>
{
    public void Configure(EntityTypeBuilder<SupportTicketEventRecord> builder)
    {
        builder.ToTable("support_ticket_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActorUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
    }
}
