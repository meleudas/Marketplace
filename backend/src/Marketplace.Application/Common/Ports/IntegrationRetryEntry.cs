namespace Marketplace.Application.Common.Ports;

public sealed record IntegrationRetryEntry(
    Guid Id,
    string Kind,
    string AggregateType,
    string AggregateId,
    string PayloadJson,
    int Attempts,
    string? LastError,
    DateTime? NextAttemptAtUtc,
    DateTime? DeadLetteredAtUtc,
    string? DeadLetterCategory,
    DateTime CreatedAtUtc);

public sealed record IntegrationRetryUpsert(
    string Kind,
    string AggregateType,
    string AggregateId,
    string PayloadJson,
    string Error);
