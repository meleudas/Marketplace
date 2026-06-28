using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class CompanyCommissionRateConfiguration : IEntityTypeConfiguration<CompanyCommissionRateRecord>
{
    public void Configure(EntityTypeBuilder<CompanyCommissionRateRecord> builder)
    {
        builder.ToTable("company_commission_rates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CommissionPercent).HasColumnType("numeric(7,4)");
        builder.Property(x => x.Reason).HasMaxLength(512);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CompanyId, x.EffectiveFrom });
        builder.HasIndex(x => new { x.CompanyId, x.EffectiveTo });
        builder.HasIndex(x => new { x.CompanyId, x.ContractId, x.EffectiveFrom });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
