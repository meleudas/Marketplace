using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SellerPayoutConfiguration : IEntityTypeConfiguration<SellerPayoutRecord>
{
    public void Configure(EntityTypeBuilder<SellerPayoutRecord> builder)
    {
        builder.ToTable("seller_payouts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ProviderReference).HasMaxLength(256);
        builder.Property(x => x.Iban).HasMaxLength(34);
        builder.Property(x => x.RecipientName).HasMaxLength(255);
        builder.Property(x => x.FailureReason).HasMaxLength(1024);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.SettlementBatchId).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
