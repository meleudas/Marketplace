using System.Text.Json;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Support.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Services;

public sealed class SupportHelpdeskOutboxPublisher
{
    private readonly IOutboxWriter _outbox;
    private readonly SupportOptions _options;

    public SupportHelpdeskOutboxPublisher(IOutboxWriter outbox, IOptions<SupportOptions> options)
    {
        _outbox = outbox;
        _options = options.Value;
    }

    public Task PublishTicketCreatedAsync(long ticketId, CancellationToken ct = default)
    {
        if (!_options.HelpdeskSyncEnabled)
            return Task.CompletedTask;

        return _outbox.AppendAsync(
            "SupportTicket",
            ticketId.ToString(),
            "SupportTicketCreated",
            JsonSerializer.Serialize(new { ticketId, provider = _options.HelpdeskProvider }),
            ct);
    }

    public Task PublishMessageAddedAsync(long ticketId, long messageId, CancellationToken ct = default)
    {
        if (!_options.HelpdeskSyncEnabled)
            return Task.CompletedTask;

        return _outbox.AppendAsync(
            "SupportTicket",
            ticketId.ToString(),
            "SupportTicketMessageAdded",
            JsonSerializer.Serialize(new { ticketId, messageId, provider = _options.HelpdeskProvider }),
            ct);
    }

    public Task PublishStatusChangedAsync(long ticketId, short status, CancellationToken ct = default)
    {
        if (!_options.HelpdeskSyncEnabled)
            return Task.CompletedTask;

        return _outbox.AppendAsync(
            "SupportTicket",
            ticketId.ToString(),
            "SupportTicketStatusChanged",
            JsonSerializer.Serialize(new { ticketId, status, provider = _options.HelpdeskProvider }),
            ct);
    }
}
