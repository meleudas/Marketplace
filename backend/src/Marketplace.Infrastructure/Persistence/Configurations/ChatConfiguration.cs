using Marketplace.Domain.Chats.Enums;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ChatConfiguration : IEntityTypeConfiguration<ChatRecord>
{
    public void Configure(EntityTypeBuilder<ChatRecord> builder)
    {
        builder.ToTable("chats");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.InitiatorUserId).IsRequired();
        builder.Property(x => x.LastMessageText).HasMaxLength(500);
        builder.Property(x => x.Meta).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ParticipantsSnapshot).HasColumnType("jsonb");
        builder.Property(x => x.RawPayload).HasColumnType("jsonb");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.Type, x.ProductId, x.InitiatorUserId, x.Status });
        builder.HasIndex(x => new { x.Type, x.OrderId, x.InitiatorUserId, x.Status });
        builder.HasIndex(x => new { x.Type, x.InitiatorUserId, x.Status });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
