using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservationRecord>
{
    public void Configure(EntityTypeBuilder<InventoryReservationRecord> builder)
    {
        builder.ToTable("inventory_reservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReservationCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(128);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CompanyId, x.ReservationCode }).IsUnique();
        builder.HasIndex(x => new { x.ExpiresAt, x.Status });
        builder.HasIndex(x => x.IsDeleted);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
