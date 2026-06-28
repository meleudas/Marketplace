using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ChatReadStateConfiguration : IEntityTypeConfiguration<ChatReadStateRecord>
{
    public void Configure(EntityTypeBuilder<ChatReadStateRecord> builder)
    {
        builder.ToTable("chat_read_states");
        builder.HasKey(x => new { x.ChatId, x.UserId });

        builder.Property(x => x.LastReadMessageId).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
