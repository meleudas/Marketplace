using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SettlementBatchConfiguration : IEntityTypeConfiguration<SettlementBatchRecord>
{
    public void Configure(EntityTypeBuilder<SettlementBatchRecord> builder)
    {
        builder.ToTable("settlement_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasIndex(x => x.PeriodEndUtc);
    }
}
