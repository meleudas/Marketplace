using Marketplace.API.Extensions;
using Marketplace.Application.Behavior.DTOs;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Behavior.Queries.GetBehaviorSummary;
using Marketplace.Application.Behavior.Queries.GetConversionFunnel;
using Marketplace.Application.Behavior.Queries.GetTopQueries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/analytics/kpi")]
[Authorize(Roles = "Admin")]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly BehaviorAnalyticsOptions _options;

    public AdminAnalyticsController(ISender sender, IOptions<BehaviorAnalyticsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (!_options.AdminAnalyticsReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        var result = await _sender.Send(new GetBehaviorSummaryQuery(from, to), ct);
        return result.ToActionResult();
    }

    [HttpGet("funnel")]
    public async Task<IActionResult> Funnel([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (!_options.AdminAnalyticsReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        var result = await _sender.Send(new GetConversionFunnelQuery(from, to), ct);
        return result.ToActionResult();
    }

    [HttpGet("top-queries")]
    public async Task<IActionResult> TopQueries([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (!_options.AdminAnalyticsReadEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        var result = await _sender.Send(new GetTopQueriesQuery(from, to, limit), ct);
        return result.ToActionResult();
    }
}
