using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Reports.Commands.AssignReportCase;
using Marketplace.Application.Reports.Commands.BulkModerationAction;
using Marketplace.Application.Reports.Commands.EscalateReportCase;
using Marketplace.Application.Reports.Commands.ResolveReportCase;
using Marketplace.Application.Reports.Options;
using Marketplace.Application.Reports.Queries.GetModerationQueue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("AdminReports")]
[Route("admin/reports")]
[Authorize(Roles = "Moderator,Admin,Support")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ReportsOptions _options;

    public AdminReportsController(ISender sender, IOptions<ReportsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (!_options.ModerationEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        var result = await _sender.Send(new GetModerationQueueQuery(limit), ct);
        MarketplaceMetrics.ReportQueueBacklog.Add(result.Value?.Count ?? 0);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/assign")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignReportCaseRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new AssignReportCaseCommand(id, actorId.ToString(), request.ModeratorUserId, request.Reason), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/resolve")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Resolve(long id, [FromBody] ResolveReportCaseRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReportResolutionLatencyMs, new KeyValuePair<string, object?>("operation", "resolve_report"));
        var result = await _sender.Send(new ResolveReportCaseCommand(id, actorId.ToString(), request.Resolution, request.CloseImmediately), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/escalate")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Escalate(long id, [FromBody] EscalateReportCaseRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new EscalateReportCaseCommand(id, actorId.ToString(), request.Reason), ct);
        if (result.IsSuccess)
            MarketplaceMetrics.ReportSlaBreaches.Add(1);
        return result.ToActionResult();
    }

    [HttpPost("bulk-actions")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> BulkActions([FromBody] BulkModerationActionRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new BulkModerationActionCommand(actorId.ToString(), request.Action, request.ReportIds, request.Reason), ct);
        return result.ToActionResult();
    }
}

public sealed record AssignReportCaseRequest(string ModeratorUserId, string Reason);
public sealed record ResolveReportCaseRequest(string Resolution, bool CloseImmediately = true);
public sealed record EscalateReportCaseRequest(string Reason);
public sealed record BulkModerationActionRequest(string Action, IReadOnlyList<long> ReportIds, string Reason);
