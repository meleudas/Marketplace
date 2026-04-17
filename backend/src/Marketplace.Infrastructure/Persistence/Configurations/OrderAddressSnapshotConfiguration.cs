using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class OrderAddressSnapshotConfiguration : IEntityTypeConfiguration<OrderAddressSnapshotRecord>
{
    public void Configure(EntityTypeBuilder<OrderAddressSnapshotRecord> builder)
    {
        builder.ToTable("order_addresses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(255).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.Kind });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
