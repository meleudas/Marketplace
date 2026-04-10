using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class CompanyMemberConfiguration : IEntityTypeConfiguration<CompanyMemberRecord>
{
    public void Configure(EntityTypeBuilder<CompanyMemberRecord> builder)
    {
        builder.ToTable("company_members");
        builder.HasKey(x => new { x.CompanyId, x.UserId });

        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.PermissionsRaw).HasColumnType("jsonb");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Role);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
