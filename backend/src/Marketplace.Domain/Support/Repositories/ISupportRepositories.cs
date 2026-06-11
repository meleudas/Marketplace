using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Domain.Support.Repositories;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken ct = default);
    Task<SupportTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> ListByUserAsync(string userId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByUserAsync(string userId, CancellationToken ct = default);
    Task<int> CountRecentByUserAsync(string userId, DateTime sinceUtc, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> ListPendingSyncAsync(int limit, CancellationToken ct = default);
    Task<SupportTicket> AddAsync(SupportTicket ticket, CancellationToken ct = default);
    Task UpdateAsync(SupportTicket ticket, CancellationToken ct = default);
}

public interface ISupportTicketMessageRepository
{
    Task<IReadOnlyList<SupportTicketMessage>> ListByTicketAsync(SupportTicketId ticketId, bool includeInternal, CancellationToken ct = default);
    Task<SupportTicketMessage> AddAsync(SupportTicketMessage message, CancellationToken ct = default);
}

public interface ISupportTicketAssignmentRepository
{
    Task<SupportTicketAssignment> AppendAsync(SupportTicketAssignment assignment, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicketAssignment>> ListByTicketAsync(SupportTicketId ticketId, int limit, CancellationToken ct = default);
}

public interface ISupportTicketEventRepository
{
    Task<SupportTicketEvent> AppendAsync(SupportTicketEvent ticketEvent, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicketEvent>> ListByTicketAsync(SupportTicketId ticketId, int limit, CancellationToken ct = default);
}

public interface ISupportExternalLinkRepository
{
    Task<SupportExternalLink?> GetByTicketAsync(SupportTicketId ticketId, string provider, CancellationToken ct = default);
    Task<SupportExternalLink?> GetByExternalIdAsync(string provider, string externalTicketId, CancellationToken ct = default);
    Task<SupportExternalLink> UpsertAsync(SupportExternalLink link, CancellationToken ct = default);
    Task<IReadOnlyList<SupportExternalLink>> ListOutOfSyncAsync(int limit, CancellationToken ct = default);
}
