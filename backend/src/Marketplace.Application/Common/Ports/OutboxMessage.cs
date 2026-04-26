namespace Marketplace.Application.Common.Ports;

public sealed record OutboxMessage(
    Guid Id,
    string AggregateType,
    string AggregateId,
    string EventType,
    string Payload,
    DateTime OccurredAtUtc,
    DateTime? ProcessedAtUtc,
    int Attempts,
    string? LastError,
    DateTime? NextAttemptAtUtc);
