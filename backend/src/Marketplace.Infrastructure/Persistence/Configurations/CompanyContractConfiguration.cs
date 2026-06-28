using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class CompanyContractConfiguration : IEntityTypeConfiguration<CompanyContractRecord>
{
    public void Configure(EntityTypeBuilder<CompanyContractRecord> builder)
    {
        builder.ToTable("company_contracts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ContractNumber).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.CompanyId, x.ContractNumber }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
