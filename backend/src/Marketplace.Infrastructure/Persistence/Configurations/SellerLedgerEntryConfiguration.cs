using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SellerLedgerEntryConfiguration : IEntityTypeConfiguration<SellerLedgerEntryRecord>
{
    public void Configure(EntityTypeBuilder<SellerLedgerEntryRecord> builder)
    {
        builder.ToTable("seller_ledger_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Description).HasMaxLength(512);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.EntryType });
        builder.HasIndex(x => x.SettlementBatchId);
        builder.HasIndex(x => x.SellerPayoutId);
        builder.HasIndex(x => new { x.CompanyId, x.Status });
    }
}
