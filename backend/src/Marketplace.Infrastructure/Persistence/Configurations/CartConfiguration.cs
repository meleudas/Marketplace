using Marketplace.Domain.Cart.Enums;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<CartRecord>
{
    public void Configure(EntityTypeBuilder<CartRecord> builder)
    {
        builder.ToTable("carts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasDefaultValue((short)CartStatus.Active);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.UserId, x.Status }).IsUnique();
        builder.HasIndex(x => x.LastActivityAt);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
