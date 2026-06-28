using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class SearchQueryAggregateConfiguration : IEntityTypeConfiguration<SearchQueryAggregateRecord>
{
    public void Configure(EntityTypeBuilder<SearchQueryAggregateRecord> builder)
    {
        builder.ToTable("search_query_aggregates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Query).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => new { x.Date, x.Query }).IsUnique();
    }
}
