using System.Text.Json;
using FluentValidation;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Commands.SyncTicketFromHelpdeskWebhook;

public sealed record SyncTicketFromHelpdeskWebhookCommand(
    string EventId,
    string PayloadJson) : IRequest<Result>;

public sealed class SyncTicketFromHelpdeskWebhookCommandValidator : AbstractValidator<SyncTicketFromHelpdeskWebhookCommand>
{
    public SyncTicketFromHelpdeskWebhookCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.PayloadJson).NotEmpty();
    }
}

public sealed class SyncTicketFromHelpdeskWebhookCommandHandler : IRequestHandler<SyncTicketFromHelpdeskWebhookCommand, Result>
{
    private const string Consumer = "support-helpdesk-webhook";

    private readonly IInboxDeduplicator _inbox;
    private readonly ISupportExternalLinkRepository _externalLinkRepository;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly SupportOptions _options;

    public SyncTicketFromHelpdeskWebhookCommandHandler(
        IInboxDeduplicator inbox,
        ISupportExternalLinkRepository externalLinkRepository,
        ISupportTicketRepository ticketRepository,
        ISupportTicketEventRepository eventRepository,
        IOptions<SupportOptions> options)
    {
        _inbox = inbox;
        _externalLinkRepository = externalLinkRepository;
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _options = options.Value;
    }

    public async Task<Result> Handle(SyncTicketFromHelpdeskWebhookCommand request, CancellationToken ct)
    {
        if (!_options.HelpdeskWebhookEnabled)
            return Result.Failure("conflict: support webhook is disabled");

        if (!Guid.TryParse(request.EventId, out var eventId))
            return Result.Failure("unprocessable: invalid eventId");

        if (await _inbox.HasProcessedAsync(eventId, Consumer, ct))
            return Result.Success();

        using var doc = JsonDocument.Parse(request.PayloadJson);
        var root = doc.RootElement;
        var externalTicketId = TryGetString(root, "externalTicketId");
        if (string.IsNullOrWhiteSpace(externalTicketId))
            return Result.Failure("unprocessable: externalTicketId is required");

        var link = await _externalLinkRepository.GetByExternalIdAsync(_options.HelpdeskProvider, externalTicketId, ct);
        if (link is null)
            return Result.Failure("not found: external ticket link not found");

        var updatedAt = TryGetDateTime(root, "updatedAt");
        var sequence = TryGetLong(root, "eventSequence") ?? 0;
        if (!link.ShouldApplyExternalUpdate(updatedAt, sequence))
        {
            await _inbox.MarkProcessedAsync(eventId, Consumer, "out-of-order", ct);
            return Result.Success();
        }

        var statusRaw = TryGetString(root, "status");
        var mapped = HelpdeskStatusMapper.Map(statusRaw);
        if (mapped is null)
            return Result.Failure("unprocessable: unknown helpdesk status");

        var ticket = await _ticketRepository.GetByIdAsync(link.TicketId, ct);
        if (ticket is null)
            return Result.Failure("not found: ticket not found");

        var now = DateTime.UtcNow;
        if (ticket.Status != mapped.Value)
        {
            if (!ticket.CanTransitionTo(mapped.Value))
                return Result.Failure("conflict: invalid ticket status transition from helpdesk");

            var update = ticket.UpdateStatus(mapped.Value, "helpdesk", "helpdesk webhook", now);
            if (update.IsFailure)
                return Result.Failure(update.Error ?? "conflict");

            await _ticketRepository.UpdateAsync(ticket, ct);
            await _eventRepository.AppendAsync(
                SupportTicketEvent.Create(
                    ticket.Id,
                    SupportTicketEventType.StatusChanged,
                    "helpdesk",
                    "helpdesk webhook",
                    new JsonBlob(JsonSerializer.Serialize(new { status = (short)mapped.Value, source = "helpdesk" })),
                    now),
                ct);
        }

        link.MarkSynced(externalTicketId, updatedAt, sequence, now);
        await _externalLinkRepository.UpsertAsync(link, ct);
        await _inbox.MarkProcessedAsync(eventId, Consumer, $"status={statusRaw}", ct);
        return Result.Success();
    }

    private static string? TryGetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var prop) ? prop.GetString() : null;

    private static DateTime? TryGetDateTime(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.GetString(), out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        return null;
    }

    private static long? TryGetLong(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var numeric))
            return numeric;
        return long.TryParse(prop.GetString(), out var parsed) ? parsed : null;
    }
}
