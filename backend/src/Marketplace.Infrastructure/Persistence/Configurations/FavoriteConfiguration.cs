using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<FavoriteRecord>
{
    public void Configure(EntityTypeBuilder<FavoriteRecord> builder)
    {
        builder.ToTable("favorites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PriceAtAdd).HasColumnType("numeric(14,2)");
        builder.Property(x => x.NotificationsRaw).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.MetaRaw).HasMaxLength(4000);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
