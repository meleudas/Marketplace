using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class CartStockWatchConfiguration : IEntityTypeConfiguration<CartStockWatchRecord>
{
    public void Configure(EntityTypeBuilder<CartStockWatchRecord> builder)
    {
        builder.ToTable("cart_stock_watches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        builder.HasIndex(x => x.ProductId);
    }
}
