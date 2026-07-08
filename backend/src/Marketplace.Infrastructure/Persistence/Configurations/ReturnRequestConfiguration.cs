using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequestRecord>
{
    public void Configure(EntityTypeBuilder<ReturnRequestRecord> builder)
    {
        builder.ToTable("return_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.RejectedReason).HasMaxLength(2000);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Status);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ReturnLineItemConfiguration : IEntityTypeConfiguration<ReturnLineItemRecord>
{
    public void Configure(EntityTypeBuilder<ReturnLineItemRecord> builder)
    {
        builder.ToTable("return_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasIndex(x => x.ReturnRequestId);
        builder.HasIndex(x => x.OrderItemId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
