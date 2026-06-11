using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipantRecord>
{
    public void Configure(EntityTypeBuilder<ChatParticipantRecord> builder)
    {
        builder.ToTable("chat_participants");
        builder.HasKey(x => new { x.ChatId, x.UserId });

        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.ChatId, x.IsDeleted, x.LeftAt });

        builder.HasQueryFilter(x => !x.IsDeleted && x.LeftAt == null);
    }
}
