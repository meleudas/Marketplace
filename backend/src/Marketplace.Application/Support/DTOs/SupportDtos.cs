namespace Marketplace.Application.Support.DTOs;

public sealed record SupportTicketDto(
    long Id,
    string TicketNumber,
    string Subject,
    short Status,
    short Priority,
    long? OrderId,
    Guid? CompanyId,
    string? AssignedToId,
    DateTime? LastMessageAt,
    DateTime? SlaDueAt,
    bool IsSlaBreached,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SupportTicketDetailDto(
    long Id,
    string TicketNumber,
    string Subject,
    string MessagePreview,
    short Status,
    short Priority,
    long? OrderId,
    Guid? CompanyId,
    long? CategoryId,
    string? AssignedToId,
    DateTime? LastMessageAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    DateTime? EscalatedAt,
    DateTime? SlaDueAt,
    bool IsSlaBreached,
    IReadOnlyList<SupportTicketMessageDto> Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SupportTicketMessageDto(
    long Id,
    long TicketId,
    string SenderId,
    string Message,
    bool IsInternal,
    DateTime CreatedAt);

public sealed record SupportTicketListDto(
    IReadOnlyList<SupportTicketDto> Items,
    int Total,
    int Page,
    int Size);
