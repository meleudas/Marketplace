using Marketplace.Application.Support.DTOs;
using Marketplace.Domain.Support.Entities;

namespace Marketplace.Application.Support;

internal static class SupportMappers
{
    public static SupportTicketDto ToDto(this SupportTicket entity, DateTime nowUtc) =>
        new(
            entity.Id.Value,
            entity.TicketNumber,
            entity.Subject,
            (short)entity.Status,
            (short)entity.Priority,
            entity.OrderId?.Value,
            entity.CompanyId?.Value,
            entity.AssignedToId,
            entity.LastMessageAt,
            entity.SlaDueAt,
            entity.IsSlaBreached(nowUtc),
            entity.CreatedAt,
            entity.UpdatedAt);

    public static SupportTicketMessageDto ToDto(this SupportTicketMessage entity) =>
        new(
            entity.Id.Value,
            entity.TicketId.Value,
            entity.SenderId,
            entity.Message,
            entity.IsInternal,
            entity.CreatedAt);

    public static SupportTicketDetailDto ToDetailDto(
        this SupportTicket entity,
        IReadOnlyList<SupportTicketMessage> messages,
        DateTime nowUtc) =>
        new(
            entity.Id.Value,
            entity.TicketNumber,
            entity.Subject,
            entity.Message,
            (short)entity.Status,
            (short)entity.Priority,
            entity.OrderId?.Value,
            entity.CompanyId?.Value,
            entity.CategoryId?.Value,
            entity.AssignedToId,
            entity.LastMessageAt,
            entity.ResolvedAt,
            entity.ClosedAt,
            entity.EscalatedAt,
            entity.SlaDueAt,
            entity.IsSlaBreached(nowUtc),
            messages.Select(x => x.ToDto()).ToList(),
            entity.CreatedAt,
            entity.UpdatedAt);
}
