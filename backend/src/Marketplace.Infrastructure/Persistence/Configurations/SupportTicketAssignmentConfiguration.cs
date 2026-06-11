using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketAssignmentConfiguration : IEntityTypeConfiguration<SupportTicketAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<SupportTicketAssignmentRecord> builder)
    {
        builder.ToTable("support_ticket_assignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssigneeUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AssignedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();

        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
    }
}
