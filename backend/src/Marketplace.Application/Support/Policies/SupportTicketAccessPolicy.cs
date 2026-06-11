using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Repositories;

namespace Marketplace.Application.Support.Policies;

public sealed class SupportTicketAccessPolicy
{
    private readonly ISupportTicketRepository _tickets;

    public SupportTicketAccessPolicy(ISupportTicketRepository tickets) => _tickets = tickets;

    public async Task<bool> CanReadAsync(SupportTicket ticket, string actorUserId, bool isStaff, CancellationToken ct = default)
    {
        if (isStaff)
            return true;
        return string.Equals(ticket.UserId, actorUserId, StringComparison.Ordinal);
    }

    public async Task<bool> CanWriteMessageAsync(SupportTicket ticket, string actorUserId, bool isStaff, bool isInternal, CancellationToken ct = default)
    {
        if (isInternal)
            return isStaff;
        if (isStaff)
            return true;
        return string.Equals(ticket.UserId, actorUserId, StringComparison.Ordinal);
    }

    public Task<bool> CanManageAsync(bool isStaff) => Task.FromResult(isStaff);

    public async Task<SupportTicket?> GetAccessibleTicketAsync(long ticketId, string actorUserId, bool isStaff, CancellationToken ct = default)
    {
        var ticket = await _tickets.GetByIdAsync(Domain.Common.ValueObjects.SupportTicketId.From(ticketId), ct);
        if (ticket is null)
            return null;
        if (!await CanReadAsync(ticket, actorUserId, isStaff, ct))
            return null;
        return ticket;
    }
}
