using Marketplace.Application.Support.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Support;

public sealed class LoggingHelpdeskPort : IHelpdeskPort
{
    private readonly ILogger<LoggingHelpdeskPort> _logger;

    public LoggingHelpdeskPort(ILogger<LoggingHelpdeskPort> logger) => _logger = logger;

    public Task<HelpdeskCreateResult> CreateTicketAsync(HelpdeskCreateRequest request, CancellationToken ct = default)
    {
        var externalId = $"logging-{request.TicketId}";
        _logger.LogInformation(
            "Helpdesk stub create ticket {TicketNumber} -> {ExternalId}",
            request.TicketNumber,
            externalId);
        return Task.FromResult(new HelpdeskCreateResult(externalId));
    }

    public Task AddCommentAsync(HelpdeskCommentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Helpdesk stub comment on {ExternalId}", request.ExternalTicketId);
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(HelpdeskStatusRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Helpdesk stub status {Status} on {ExternalId}", request.Status, request.ExternalTicketId);
        return Task.CompletedTask;
    }

    public Task<HelpdeskTicketSnapshot?> FetchTicketSnapshotAsync(string externalTicketId, CancellationToken ct = default)
    {
        _logger.LogInformation("Helpdesk stub fetch {ExternalId}", externalTicketId);
        return Task.FromResult<HelpdeskTicketSnapshot?>(new HelpdeskTicketSnapshot(
            externalTicketId,
            "open",
            DateTime.UtcNow,
            DateTime.UtcNow.Ticks));
    }
}
