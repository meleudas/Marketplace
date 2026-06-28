using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessageRecord>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessageRecord> builder)
    {
        builder.ToTable("support_ticket_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.Attachments).HasColumnType("jsonb");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
