using Marketplace.API.Extensions;
using Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;
using Marketplace.Application.Behavior.Commands.TrackSearchQuery;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly BehaviorAnalyticsOptions _options;

    public AnalyticsController(ISender sender, IOptions<BehaviorAnalyticsOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost("events")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventRequest request, CancellationToken ct)
    {
        if (!_options.BehaviorTrackingEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if ((request.Payload?.Length ?? 0) > _options.PayloadMaxBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.AnalyticsPipelineLatencyMs, new KeyValuePair<string, object?>("operation", "ingest_event"));
        var result = await _sender.Send(
            new TrackCatalogInteractionCommand(request.UserId, request.SessionId, request.EventType, request.Source, request.Payload ?? "{}", request.ConsentGranted),
            ct);
        Track(result.IsSuccess);
        return result.ToActionResult();
    }

    [HttpPost("search-history")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackSearch([FromBody] TrackSearchRequest request, CancellationToken ct)
    {
        if (!_options.BehaviorTrackingEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if ((request.Payload?.Length ?? 0) > _options.PayloadMaxBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        var result = await _sender.Send(
            new TrackSearchQueryCommand(request.UserId, request.SessionId, request.Query, request.Payload ?? "{}", request.ConsentGranted),
            ct);
        Track(result.IsSuccess);
        return result.ToActionResult();
    }

    private static void Track(bool success)
    {
        if (success)
            MarketplaceMetrics.AnalyticsEventsIngested.Add(1);
        else
            MarketplaceMetrics.AnalyticsEventsDropped.Add(1);
    }
}

public sealed record TrackEventRequest(Guid? UserId, string SessionId, short EventType, string Source, string? Payload, bool? ConsentGranted);
public sealed record TrackSearchRequest(Guid? UserId, string SessionId, string Query, string? Payload, bool? ConsentGranted);
