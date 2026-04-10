using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovementRecord>
{
    public void Configure(EntityTypeBuilder<StockMovementRecord> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(128);
        builder.Property(x => x.Reason).HasMaxLength(512);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CompanyId, x.ProductId, x.CreatedAt });
        builder.HasIndex(x => new { x.CompanyId, x.OperationId }).IsUnique();
        builder.HasIndex(x => x.IsDeleted);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
