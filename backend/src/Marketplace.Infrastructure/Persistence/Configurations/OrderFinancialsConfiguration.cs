using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class OrderFinancialsConfiguration : IEntityTypeConfiguration<OrderFinancialsRecord>
{
    public void Configure(EntityTypeBuilder<OrderFinancialsRecord> builder)
    {
        builder.ToTable("order_financials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.MerchandiseSubtotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.MerchandiseBase).HasColumnType("numeric(18,2)");
        builder.Property(x => x.CommissionPercent).HasColumnType("numeric(7,4)");
        builder.Property(x => x.PlatformFee).HasColumnType("numeric(18,2)");
        builder.Property(x => x.SellerMerchandiseNet).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ShippingAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.SellerPayoutEligible).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.HasIndex(x => x.CompanyId);
    }
}
