using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.CreateProductReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnProductReview;
using Marketplace.Application.Reviews.Queries.GetProductReviews;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("products/{productId:long}/reviews")]
public sealed class ProductReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductReviewsController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(long productId, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_list_product"));
        var result = await _sender.Send(new GetProductReviewsQuery(productId, page, size), ct);
        TrackReviewResult("reviews_list_product", result);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(long productId, [FromBody] UpsertProductReviewRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_create_product"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_create_product"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var userName = User.Identity?.Name ?? actorId.ToString();
        var result = await _sender.Send(new CreateProductReviewCommand(
            productId,
            actorId,
            userName,
            null,
            request.Rating,
            request.Title,
            request.Comment), ct);
        TrackReviewResult("reviews_create_product", result);
        return result.ToActionResult();
    }

    [HttpPatch("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Update(long productId, long reviewId, [FromBody] UpsertProductReviewRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_update_product"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_update_product"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new UpdateOwnProductReviewCommand(productId, reviewId, actorId, request.Rating, request.Title, request.Comment), ct);
        TrackReviewResult("reviews_update_product", result);
        return result.ToActionResult();
    }

    [HttpDelete("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(long productId, long reviewId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_delete_product"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_delete_product"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new DeleteOwnProductReviewCommand(productId, reviewId, actorId), ct);
        TrackReviewResult("reviews_delete_product", result);
        return result.ToActionResult();
    }

    private static void TrackReviewResult(string operation, Marketplace.Domain.Shared.Kernel.Result result)
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

public sealed record UpsertProductReviewRequest(byte Rating, string? Title, string Comment);
