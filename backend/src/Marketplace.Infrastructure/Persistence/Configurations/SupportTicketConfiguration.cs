using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicketRecord>
{
    public void Configure(EntityTypeBuilder<SupportTicketRecord> builder)
    {
        builder.ToTable("support_tickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TicketNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.AssignedToId).HasMaxLength(64);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
