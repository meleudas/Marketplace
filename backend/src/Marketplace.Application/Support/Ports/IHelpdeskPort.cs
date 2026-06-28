namespace Marketplace.Application.Support.Ports;

public interface IHelpdeskPort
{
    Task<HelpdeskCreateResult> CreateTicketAsync(HelpdeskCreateRequest request, CancellationToken ct = default);
    Task AddCommentAsync(HelpdeskCommentRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(HelpdeskStatusRequest request, CancellationToken ct = default);
    Task<HelpdeskTicketSnapshot?> FetchTicketSnapshotAsync(string externalTicketId, CancellationToken ct = default);
}

public sealed record HelpdeskCreateRequest(
    long TicketId,
    string TicketNumber,
    string Subject,
    string Message,
    short Priority,
    string UserId);

public sealed record HelpdeskCreateResult(string ExternalTicketId);

public sealed record HelpdeskCommentRequest(
    string ExternalTicketId,
    long MessageId,
    string Text,
    bool IsInternal);

public sealed record HelpdeskStatusRequest(
    string ExternalTicketId,
    short Status);

public sealed record HelpdeskTicketSnapshot(
    string ExternalTicketId,
    string Status,
    DateTime? UpdatedAtUtc,
    long Sequence);
