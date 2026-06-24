namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class IntegrationRetryRecord
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? DeadLetteredAtUtc { get; set; }
    public string? DeadLetterReason { get; set; }
    public string? DeadLetterCategory { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
