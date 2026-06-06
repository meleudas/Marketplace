using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.ModerateCompanyReview;
using Marketplace.Application.Reviews.Commands.ModerateProductReview;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/reviews")]
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminReviewsController(ISender sender) => _sender = sender;

    [HttpPost("products/{reviewId:long}/moderate")]
    public async Task<IActionResult> ModerateProduct(long reviewId, [FromBody] ModerateProductReviewRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_moderate_product"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_moderate_product"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");
        var result = await _sender.Send(new ModerateProductReviewCommand(reviewId, actorId, canModerate, request.Status), ct);
        TrackReviewResult("reviews_moderate_product", result);
        return result.ToActionResult();
    }

    [HttpPost("companies/{reviewId:long}/moderate")]
    public async Task<IActionResult> ModerateCompany(long reviewId, [FromBody] ModerateCompanyReviewRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ReviewLatencyMs, new KeyValuePair<string, object?>("operation", "reviews_moderate_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ReviewErrors.Add(1, [new KeyValuePair<string, object?>("operation", "reviews_moderate_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");
        var result = await _sender.Send(new ModerateCompanyReviewCommand(reviewId, actorId, canModerate, request.Status), ct);
        TrackReviewResult("reviews_moderate_company", result);
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

public sealed record ModerateProductReviewRequest(ReviewModerationStatus Status);
public sealed record ModerateCompanyReviewRequest(CompanyReviewStatus Status);
