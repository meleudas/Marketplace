using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SupportExternalLinkConfiguration : IEntityTypeConfiguration<SupportExternalLinkRecord>
{
    public void Configure(EntityTypeBuilder<SupportExternalLinkRecord> builder)
    {
        builder.ToTable("support_external_links");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExternalTicketId).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.TicketId, x.Provider }).IsUnique();
        builder.HasIndex(x => new { x.Provider, x.ExternalTicketId }).IsUnique();
        builder.HasIndex(x => x.SyncState);
    }
}
