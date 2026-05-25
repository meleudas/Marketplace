using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public sealed class HttpIdempotencyRequestConfiguration : IEntityTypeConfiguration<HttpIdempotencyRequestRecord>
{
    public void Configure(EntityTypeBuilder<HttpIdempotencyRequestRecord> builder)
    {
        builder.ToTable("http_idempotency_requests");
        builder.HasKey(x => new { x.Scope, x.IdempotencyKey });
        builder.Property(x => x.Scope).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResponseBodyJson).HasMaxLength(16000);
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasIndex(x => x.CompletedAtUtc);
    }
}
