using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ChatModerationActionConfiguration : IEntityTypeConfiguration<ChatModerationActionRecord>
{
    public void Configure(EntityTypeBuilder<ChatModerationActionRecord> builder)
    {
        builder.ToTable("chat_moderation_actions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ActionType).IsRequired();

        builder.HasIndex(x => new { x.ChatId, x.CreatedAt });
    }
}
