using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class CompanyLegalProfileConfiguration : IEntityTypeConfiguration<CompanyLegalProfileRecord>
{
    public void Configure(EntityTypeBuilder<CompanyLegalProfileRecord> builder)
    {
        builder.ToTable("company_legal_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.LegalName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Edrpou).HasMaxLength(10);
        builder.Property(x => x.Ipn).HasMaxLength(9);
        builder.Property(x => x.CertificateNumber).HasMaxLength(50);
        builder.Property(x => x.InitialCommissionPercent).HasColumnType("numeric(7,4)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.CompanyId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
