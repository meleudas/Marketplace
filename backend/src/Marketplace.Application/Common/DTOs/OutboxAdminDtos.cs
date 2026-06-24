namespace Marketplace.Application.Common.DTOs;

public sealed record OutboxMessageAdminDto(
    Guid Id,
    string AggregateType,
    string EventType,
    int Attempts,
    string? LastError,
    string? DeadLetterCategory,
    DateTime OccurredAtUtc);

public sealed record PagedOutboxMessagesDto(
    IReadOnlyList<OutboxMessageAdminDto> Items,
    long Total,
    int Page,
    int PageSize);
