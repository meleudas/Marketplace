using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.CreateCompanyReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnCompanyReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnCompanyReview;
using Marketplace.Application.Reviews.Queries.GetCompanyReviews;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Reviews")]
[Route("companies/{companyId:guid}/reviews")]
public sealed class CompanyReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public CompanyReviewsController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(Guid companyId, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_list_company"));
        var result = await _sender.Send(new GetCompanyReviewsQuery(companyId, page, size), ct);
        TrackReviewResult("reviews_list_company", result);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] UpsertCompanyReviewRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_create_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_create_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var userName = User.Identity?.Name ?? actorId.ToString();
        var result = await _sender.Send(new CreateCompanyReviewCommand(
            companyId,
            actorId,
            userName,
            request.OverallRating,
            request.Comment), ct);
        TrackReviewResult("reviews_create_company", result);
        return result.ToActionResult();
    }

    [HttpPatch("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid companyId, long reviewId, [FromBody] UpsertCompanyReviewRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_update_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_update_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new UpdateOwnCompanyReviewCommand(companyId, reviewId, actorId, request.OverallRating, request.Comment), ct);
        TrackReviewResult("reviews_update_company", result);
        return result.ToActionResult();
    }

    [HttpDelete("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid companyId, long reviewId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_delete_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_delete_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new DeleteOwnCompanyReviewCommand(companyId, reviewId, actorId), ct);
        TrackReviewResult("reviews_delete_company", result);
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

public sealed record UpsertCompanyReviewRequest(decimal OverallRating, string Comment);
