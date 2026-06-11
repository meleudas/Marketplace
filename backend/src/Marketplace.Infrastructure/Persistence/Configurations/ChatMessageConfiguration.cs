using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessageRecord>
{
    public void Configure(EntityTypeBuilder<ChatMessageRecord> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.Attachments).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.DeletedBy).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RawPayload).HasColumnType("jsonb");
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.ChatId, x.CreatedAt });
        builder.HasIndex(x => x.SenderId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
