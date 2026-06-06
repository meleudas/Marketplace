using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.UpsertCompanyReviewReply;
using Marketplace.Application.Reviews.Commands.UpsertProductReviewReply;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("reviews")]
[Authorize]
public sealed class ReviewRepliesController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewRepliesController(ISender sender) => _sender = sender;

    [HttpPut("products/{reviewId:long}/reply")]
    public async Task<IActionResult> UpsertProductReply(long reviewId, [FromBody] UpsertReviewReplyRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_reply_product"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_reply_product"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new UpsertProductReviewReplyCommand(
            reviewId,
            actorId,
            User.IsInRole("Admin"),
            request.Body), ct);
        TrackReviewResult("reviews_reply_product", result);
        return result.ToActionResult();
    }

    [HttpPut("companies/{reviewId:long}/reply")]
    public async Task<IActionResult> UpsertCompanyReply(long reviewId, [FromBody] UpsertReviewReplyRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_reply_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_reply_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new UpsertCompanyReviewReplyCommand(
            reviewId,
            actorId,
            User.IsInRole("Admin"),
            request.Body), ct);
        TrackReviewResult("reviews_reply_company", result);
        return result.ToActionResult();
    }

    private static void TrackReviewResult<T>(string operation, Marketplace.Domain.Shared.Kernel.Result<T> result)
    {
        if (result.IsSuccess)
        {
            MarketplaceMetrics.ReviewOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        var reason = string.Equals(result.Error, "Forbidden", StringComparison.OrdinalIgnoreCase)
            ? "forbidden"
            : string.Equals(result.Error, "Review not found", StringComparison.OrdinalIgnoreCase)
                ? "not_found"
                : "application_failure";
        MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", reason)]);
    }
}

public sealed record UpsertReviewReplyRequest(string Body);
