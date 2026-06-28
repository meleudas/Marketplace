using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Ports;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class SupportHelpdeskReconciliationJobs
{
    private readonly IHelpdeskPort _helpdesk;
    private readonly ISupportExternalLinkRepository _externalLinkRepository;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly IOutboxWriter _outbox;
    private readonly SupportOptions _options;

    public SupportHelpdeskReconciliationJobs(
        IHelpdeskPort helpdesk,
        ISupportExternalLinkRepository externalLinkRepository,
        ISupportTicketRepository ticketRepository,
        ISupportTicketEventRepository eventRepository,
        IOutboxWriter outbox,
        IOptions<SupportOptions> options)
    {
        _helpdesk = helpdesk;
        _externalLinkRepository = externalLinkRepository;
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _outbox = outbox;
        _options = options.Value;
    }

    public Task ReconcileAsync(CancellationToken ct = default) =>
        MarketplaceTelemetry.RunJobAsync("support-helpdesk-reconcile", ReconcileCoreAsync, ct);

    private async Task ReconcileCoreAsync(CancellationToken ct)
    {
        if (!_options.HelpdeskSyncEnabled)
            return;

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.SupportTicketLatencyMs,
            new KeyValuePair<string, object?>("operation", "reconcile"));

        var links = await _externalLinkRepository.ListOutOfSyncAsync(100, ct);
        foreach (var link in links)
        {
            try
            {
                await ReconcileLinkAsync(link, ct);
            }
            catch (Exception)
            {
                MarketplaceMetrics.SupportHelpdeskSyncFailuresTotal.Add(1);
                link.MarkFailed(DateTime.UtcNow);
                await _externalLinkRepository.UpsertAsync(link, ct);
            }
        }
    }

    private async Task ReconcileLinkAsync(SupportExternalLink link, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(link.ExternalTicketId))
        {
            await _outbox.AppendAsync(
                "SupportTicket",
                link.TicketId.Value.ToString(),
                "SupportTicketCreated",
                System.Text.Json.JsonSerializer.Serialize(new { ticketId = link.TicketId.Value, provider = _options.HelpdeskProvider }),
                ct);
            return;
        }

        var snapshot = await _helpdesk.FetchTicketSnapshotAsync(link.ExternalTicketId, ct);
        if (snapshot is null)
        {
            link.MarkFailed(now);
            await _externalLinkRepository.UpsertAsync(link, ct);
            return;
        }

        if (!link.ShouldApplyExternalUpdate(snapshot.UpdatedAtUtc, snapshot.Sequence))
        {
            link.MarkSynced(link.ExternalTicketId, snapshot.UpdatedAtUtc, snapshot.Sequence, now);
            await _externalLinkRepository.UpsertAsync(link, ct);
            return;
        }

        var ticket = await _ticketRepository.GetByIdAsync(link.TicketId, ct);
        if (ticket is null)
            return;

        var mapped = HelpdeskStatusMapper.Map(snapshot.Status);
        if (mapped.HasValue && ticket.Status != mapped.Value && ticket.CanTransitionTo(mapped.Value))
        {
            ticket.UpdateStatus(mapped.Value, "helpdesk-reconcile", "reconciliation", now);
            await _ticketRepository.UpdateAsync(ticket, ct);
            await _eventRepository.AppendAsync(
                SupportTicketEvent.Create(
                    ticket.Id,
                    SupportTicketEventType.StatusChanged,
                    "helpdesk-reconcile",
                    "reconciliation",
                    new Domain.Common.ValueObjects.JsonBlob(
                        System.Text.Json.JsonSerializer.Serialize(new { status = (short)mapped.Value })),
                    now),
                ct);
        }

        link.MarkSynced(link.ExternalTicketId, snapshot.UpdatedAtUtc, snapshot.Sequence, now);
        await _externalLinkRepository.UpsertAsync(link, ct);
    }
}
