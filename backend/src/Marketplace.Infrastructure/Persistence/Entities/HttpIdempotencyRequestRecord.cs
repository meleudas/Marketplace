namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class HttpIdempotencyRequestRecord
{
    public string Scope { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "in_progress";
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBodyJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
