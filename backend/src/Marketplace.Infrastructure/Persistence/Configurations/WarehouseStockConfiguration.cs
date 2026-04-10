using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStockRecord>
{
    public void Configure(EntityTypeBuilder<WarehouseStockRecord> builder)
    {
        builder.ToTable("warehouse_stocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.CompanyId, x.ProductId });
        builder.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
        builder.HasIndex(x => x.IsDeleted);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
