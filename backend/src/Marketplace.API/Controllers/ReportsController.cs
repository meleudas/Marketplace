using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Reports.Commands.CreateReport;
using Marketplace.Application.Reports.Options;
using Marketplace.Application.Reports.Queries.GetMyReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Reports")]
[Route("reports")]
[Authorize(Roles = "User,Buyer")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ReportsOptions _options;

    public ReportsController(ISender sender, IOptions<ReportsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request, CancellationToken ct)
    {
        if (!_options.PublicCreateEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReportResolutionLatencyMs, new KeyValuePair<string, object?>("operation", "create_report"));
        var result = await _sender.Send(
            new CreateReportCommand(
                actorId.ToString(),
                request.TargetType,
                request.TargetId,
                request.Reason,
                request.Description,
                request.Images ?? [],
                request.Priority),
            ct);
        Track("create_report", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("/me/reports")]
    public async Task<IActionResult> GetMyReports(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetMyReportsQuery(actorId.ToString()), ct);
        return result.ToActionResult();
    }

    private static void Track(string operation, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.ReportOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.ReportErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
        if ((error ?? string.Empty).Contains("conflict", StringComparison.OrdinalIgnoreCase))
            MarketplaceMetrics.ReportQueueBacklog.Add(1);
    }
}

public sealed record CreateReportRequest(
    short TargetType,
    string TargetId,
    short Reason,
    string Description,
    short Priority,
    string[]? Images);
