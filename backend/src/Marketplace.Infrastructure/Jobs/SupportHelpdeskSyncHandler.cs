using System.Text.Json;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Ports;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class SupportHelpdeskSyncHandler : ISupportHelpdeskSyncHandler
{
    private readonly IHelpdeskPort _helpdesk;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketMessageRepository _messageRepository;
    private readonly ISupportExternalLinkRepository _externalLinkRepository;
    private readonly SupportOptions _options;

    public SupportHelpdeskSyncHandler(
        IHelpdeskPort helpdesk,
        ISupportTicketRepository ticketRepository,
        ISupportTicketMessageRepository messageRepository,
        ISupportExternalLinkRepository externalLinkRepository,
        IOptions<SupportOptions> options)
    {
        _helpdesk = helpdesk;
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _externalLinkRepository = externalLinkRepository;
        _options = options.Value;
    }

    public async Task ProcessAsync(OutboxMessage message, CancellationToken ct = default)
    {
        if (!_options.HelpdeskSyncEnabled)
            return;

        using var doc = JsonDocument.Parse(message.Payload);
        var root = doc.RootElement;
        var ticketId = root.GetProperty("ticketId").GetInt64();
        var ticket = await _ticketRepository.GetByIdAsync(SupportTicketId.From(ticketId), ct)
            ?? throw new PermanentOutboxException($"Support ticket '{ticketId}' not found.");

        try
        {
            switch (message.EventType)
            {
                case "SupportTicketCreated":
                    await SyncCreateAsync(ticket, ct);
                    break;
                case "SupportTicketMessageAdded":
                    await SyncMessageAsync(ticket, root, ct);
                    break;
                case "SupportTicketStatusChanged":
                    await SyncStatusAsync(ticket, ct);
                    break;
                default:
                    throw new PermanentOutboxException($"Unsupported support outbox event: {message.EventType}");
            }
        }
        catch (PermanentOutboxException)
        {
            throw;
        }
        catch (Exception)
        {
            MarketplaceMetrics.SupportHelpdeskSyncFailuresTotal.Add(1);
            throw;
        }
    }

    private async Task SyncCreateAsync(SupportTicket ticket, CancellationToken ct)
    {
        var link = await _externalLinkRepository.GetByTicketAsync(ticket.Id, _options.HelpdeskProvider, ct)
            ?? SupportExternalLink.CreatePending(ticket.Id, _options.HelpdeskProvider, DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(link.ExternalTicketId))
            return;

        var result = await _helpdesk.CreateTicketAsync(
            new HelpdeskCreateRequest(
                ticket.Id.Value,
                ticket.TicketNumber,
                ticket.Subject,
                ticket.Message,
                (short)ticket.Priority,
                ticket.UserId),
            ct);

        link.MarkSynced(result.ExternalTicketId, DateTime.UtcNow, DateTime.UtcNow.Ticks, DateTime.UtcNow);
        await _externalLinkRepository.UpsertAsync(link, ct);
    }

    private async Task SyncMessageAsync(SupportTicket ticket, JsonElement root, CancellationToken ct)
    {
        var link = await GetLinkOrThrowAsync(ticket, ct);
        if (!root.TryGetProperty("messageId", out var messageIdProp))
            return;

        var messageId = messageIdProp.GetInt64();
        var messages = await _messageRepository.ListByTicketAsync(ticket.Id, includeInternal: true, ct);
        var message = messages.FirstOrDefault(x => x.Id.Value == messageId)
            ?? throw new PermanentOutboxException($"Support message '{messageId}' not found.");

        await _helpdesk.AddCommentAsync(
            new HelpdeskCommentRequest(link.ExternalTicketId, messageId, message.Message, message.IsInternal),
            ct);
    }

    private async Task SyncStatusAsync(SupportTicket ticket, CancellationToken ct)
    {
        var link = await GetLinkOrThrowAsync(ticket, ct);
        await _helpdesk.UpdateStatusAsync(
            new HelpdeskStatusRequest(link.ExternalTicketId, (short)ticket.Status),
            ct);
        link.MarkSynced(link.ExternalTicketId, DateTime.UtcNow, DateTime.UtcNow.Ticks, DateTime.UtcNow);
        await _externalLinkRepository.UpsertAsync(link, ct);
    }

    private async Task<SupportExternalLink> GetLinkOrThrowAsync(SupportTicket ticket, CancellationToken ct)
    {
        var link = await _externalLinkRepository.GetByTicketAsync(ticket.Id, _options.HelpdeskProvider, ct);
        if (link is null || string.IsNullOrWhiteSpace(link.ExternalTicketId))
            throw new PermanentOutboxException($"External link missing for ticket '{ticket.Id.Value}'");
        return link;
    }
}
